using Anvil.CSharp.Collections;
using Anvil.CSharp.Core;
using Anvil.CSharp.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Unity.Entities;
using Debug = UnityEngine.Debug;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    /// <summary>
    /// Given a "Task" to complete, the TaskDriver handles ensuring it is populated, processed and completed by
    /// defining the data needed, any subtasks to accomplish and the Unity Jobs to do the work required.
    /// TaskDrivers are contextual, meaning that the work they accomplish is unique to their usage in different parts
    /// of an application or as different sub task drivers as part of larger, more complex Task Drivers.
    /// The goal of a TaskDriver is to convert the specific contextual data into general agnostic data that the corresponding
    /// <see cref="AbstractTaskDriverSystem"/> will process in parallel. The results of that system processing
    /// are then picked up by the TaskDriver to be converted to specific contextual data again and passed on to
    /// a sub task driver or to another system.
    /// </summary>
    public abstract class AbstractTaskDriver : AbstractAnvilBase, ITaskSetOwner
    {
        private static readonly Type TASK_DRIVER_SYSTEM_TYPE = typeof(TaskDriverSystem<>);
        private static readonly Type COMPONENT_SYSTEM_GROUP_TYPE = typeof(ComponentSystemGroup);
        private static readonly Type WORLD_TYPE = typeof(World);
        private static readonly MethodInfo ADD_SYSTEM_MANAGED_METHOD_INFO = WORLD_TYPE.GetMethod("AddSystemManaged", BindingFlags.Instance | BindingFlags.Public);

        private readonly PersistentDataSystem m_PersistentDataSystem;
        private readonly List<AbstractTaskDriver> m_SubTaskDrivers;

        private bool m_IsHardened;
        private bool m_HasCancellableData;

        /// <summary>
        /// Reference to the associated <see cref="World"/>.
        /// </summary>
        public World World { get; }

        internal AbstractTaskDriver Parent { get; }

        internal AbstractTaskDriverSystem TaskDriverSystem { get; }

        internal TaskSet TaskSet { get; }

        /// <summary>
        /// Data Stream representing requests to Cancel an <see cref="Entity"/>.
        /// </summary>
        public IDriverCancelRequestDataStream CancelRequestDataStream
        {
            get => TaskSet.CancelRequestsDataStream;
        }

        /// <summary>
        /// Data Stream representing when Cancel Requests are Complete.
        /// </summary>
        public IDriverDataStream<CancelComplete> CancelCompleteDataStream
        {
            get => TaskSet.CancelCompleteDataStream;
        }

        internal DataOwnerID WorldUniqueID { get; }

        /// <summary>
        /// Reference to the associated <see cref="TaskDriverSystem"/>.
        /// Generally used to create <see cref="EntityQuery"/> instances.
        /// </summary>
        public ITaskDriverSystem System
        {
            get => new ContextTaskDriverSystemWrapper(TaskDriverSystem, this);
        }

        AbstractTaskDriverSystem ITaskSetOwner.TaskDriverSystem
        {
            get => TaskDriverSystem;
        }

        TaskSet ITaskSetOwner.TaskSet
        {
            get => TaskSet;
        }

        DataOwnerID IWorldUniqueID<DataOwnerID>.WorldUniqueID
        {
            get => WorldUniqueID;
        }

        List<AbstractTaskDriver> ITaskSetOwner.SubTaskDrivers
        {
            get => m_SubTaskDrivers;
        }

        bool ITaskSetOwner.HasCancellableData
        {
            get
            {
                Debug_EnsureHardened();
                return m_HasCancellableData;
            }
        }

        /// <summary>
        /// Creates a new instance of a <see cref="AbstractTaskDriver"/>.
        /// </summary>
        /// <param name="world">The <see cref="World"/> this Task Driver is a part of.</param>
        /// <param name="parent">The parent <see cref="AbstractTaskDriver"/> if it exists</param>
        /// <param name="uniqueContextIdentifier">
        /// An optional unique identifier to identify this TaskDriver by. This is necessary when there are two or more of the
        /// same type of TaskDrivers at the same level in the hierarchy.
        /// Ex.
        /// ShootTaskDriver
        ///  - TimerTaskDriver (for time between shots)
        ///  - TimerTaskDriver (for reloading)
        ///
        /// Both TimerTaskDriver's would conflict as being siblings of the ShootTaskDriver so they would need a unique
        /// context identifier to distinguish them for ensuring migration happens properly between worlds and data
        /// goes to the correct location.
        /// </param>
        /// <param name="customSystemType">
        /// Explicitly set the system type that this instance should be bound to. The type must derive from
        /// <see cref="AbstractTaskDriverSystem"/>.
        /// This is typically used to order the task driver instance against other systems.
        ///
        /// (default) If a null value is provided <see cref="TaskDriverSystem{T}"/> is used.
        /// </param>
        protected AbstractTaskDriver(World world, AbstractTaskDriver parent = null, string uniqueContextIdentifier = null, Type customSystemType = null)
        {
            DEBUG_EnsureValidAttributes();

            World = world;
            Parent = parent;
            //TODO: #241 - This is gross. Needs to be reworked.
            Parent?.m_SubTaskDrivers.Add(this);
            WorldUniqueID = GenerateWorldUniqueID(uniqueContextIdentifier);

            TaskDriverManagementSystem taskDriverManagementSystem = World.GetOrCreateSystemManaged<TaskDriverManagementSystem>();
            m_PersistentDataSystem = World.GetOrCreateSystemManaged<PersistentDataSystem>();

            m_SubTaskDrivers = new List<AbstractTaskDriver>();
            TaskSet = new TaskSet(this);

            if (customSystemType == null)
            {
                Type taskDriverType = GetType();
                customSystemType = TASK_DRIVER_SYSTEM_TYPE.MakeGenericType(taskDriverType);
            }
            Debug.Assert(customSystemType.IsSubclassOf(typeof(AbstractTaskDriverSystem)));


            //If we've already created a TaskDriver of this type, then it's corresponding system will also have been created.
            TaskDriverSystem = (AbstractTaskDriverSystem)World.GetExistingSystemManaged(customSystemType);
            //If not, then we will want to explicitly create it and ensure it is part of the lifecycle.
            if (TaskDriverSystem == null)
            {
                TaskDriverSystem = (AbstractTaskDriverSystem)Activator.CreateInstance(customSystemType, World);
                MethodInfo customAddSystemManagedMethodInfo = ADD_SYSTEM_MANAGED_METHOD_INFO.MakeGenericMethod(customSystemType);
                customAddSystemManagedMethodInfo.Invoke(World, new object[] { TaskDriverSystem });
                ComponentSystemGroup systemGroup = GetSystemGroup();
                systemGroup.AddSystemToUpdateList(TaskDriverSystem);
            }

            TaskDriverSystem.RegisterTaskDriver(this);
            taskDriverManagementSystem.RegisterTaskDriver(this);
        }

        private DataOwnerID GenerateWorldUniqueID(string uniqueContextIdentifier)
        {
            //If we have a parent, we include their id in ours, otherwise we're top level.
            string idPath = $"{(Parent != null ? Parent.WorldUniqueID : string.Empty)}/{GetType().AssemblyQualifiedName}{uniqueContextIdentifier ?? string.Empty}";
            return new DataOwnerID(idPath.GetBurstHashCode32());
        }

        protected override void DisposeSelf()
        {
            //We own our sub task drivers so dispose them
            m_SubTaskDrivers.DisposeAllAndTryClear();

            TaskSet.Dispose();

            base.DisposeSelf();
        }

        public override string ToString()
        {
            return $"{GetType().GetReadableName()}|{WorldUniqueID}";
        }

        private ComponentSystemGroup GetSystemGroup()
        {
            Type systemGroupType = GetSystemGroupType();
            if (!COMPONENT_SYSTEM_GROUP_TYPE.IsAssignableFrom(systemGroupType))
            {
                throw new InvalidOperationException($"Tried to get the {COMPONENT_SYSTEM_GROUP_TYPE.GetReadableName()} for {this} but {systemGroupType.GetReadableName()} is not a valid group type!");
            }

            return (ComponentSystemGroup)World.GetOrCreateSystemManaged(systemGroupType);
        }

        private Type GetSystemGroupType()
        {
            Type type = GetType();
            UpdateInGroupAttribute updateInGroupAttribute = type.GetCustomAttribute<UpdateInGroupAttribute>();
            return updateInGroupAttribute == null ? typeof(SimulationSystemGroup) : updateInGroupAttribute.GroupType;
        }

        //*************************************************************************************************************
        // CONFIGURATION
        //*************************************************************************************************************

        protected IDriverDataStream<TInstance> CreateDataStream<TInstance>(CancelRequestBehaviour cancelRequestBehaviour = CancelRequestBehaviour.Delete, string uniqueContextIdentifier = null)
            where TInstance : unmanaged, IEntityKeyedTask
        {
            IDriverDataStream<TInstance> dataStream = TaskSet.CreateDataStream<TInstance>(cancelRequestBehaviour, uniqueContextIdentifier);
            return dataStream;
        }

        protected IDriverEntityPersistentData<T> CreateEntityPersistentData<T>(string uniqueContextIdentifier = null)
            where T : unmanaged, IEntityPersistentDataInstance
        {
            EntityPersistentData<T> entityPersistentData = TaskSet.CreateEntityPersistentData<T>(uniqueContextIdentifier);
            return entityPersistentData;
        }

        protected IWorldEntityPersistentData<T> GetOrCreateWorldEntityPersistentData<T>(string uniqueContextIdentifier = null)
            where T : unmanaged, IEntityPersistentDataInstance
        {
            EntityPersistentData<T> entityPersistentData = m_PersistentDataSystem.GetOrCreateEntityPersistentData<T>(uniqueContextIdentifier);
            return entityPersistentData;
        }

        protected IThreadPersistentData<T> GetOrCreateThreadPersistentData<T>(string uniqueContextIdentifier = null)
            where T : unmanaged, IThreadPersistentDataInstance
        {
            ThreadPersistentData<T> threadPersistentData = m_PersistentDataSystem.GetOrCreateThreadPersistentData<T>(uniqueContextIdentifier);
            return threadPersistentData;
        }

        //*************************************************************************************************************
        // JOB CONFIGURATION - DRIVER LEVEL
        //*************************************************************************************************************

        /// <summary>
        /// Configures a Job that is triggered by instances being present in the passed in <see cref="IDriverDataStream{TInstance}"/>.
        /// </summary>
        /// <param name="dataStream">The <see cref="IDriverDataStream{TInstance}"/> to trigger the job off of.</param>
        /// <param name="scheduleJobFunction">The scheduling function to call to schedule the job.</param>
        /// <param name="batchStrategy">The <see cref="BatchStrategy"/> to use for executing the job.</param>
        /// <typeparam name="TInstance">The type of instance contained in the <see cref="IDriverDataStream{TInstance}"/></typeparam>
        /// <returns>A <see cref="IJobConfig"/> to allow for chaining more configuration options.</returns>
        protected IJobConfig ConfigureJobTriggeredBy<TInstance>(
            IDriverDataStream<TInstance> dataStream,
            JobConfigScheduleDelegates.ScheduleDataStreamJobDelegate<TInstance> scheduleJobFunction,
            BatchStrategy batchStrategy)
            where TInstance : unmanaged, IEntityKeyedTask
        {
            return TaskSet.ConfigureJobTriggeredBy(
                (EntityProxyDataStream<TInstance>)dataStream,
                scheduleJobFunction,
                batchStrategy);
        }

        /// <summary>
        /// Configures an <see cref="ITaskCancelJobForDefer{TInstance}"/> job to be run. This will operate
        /// on data in a stream that has been requested to cancel with <see cref="CancelRequestBehaviour.Unwind"/>. It
        /// provides the opportunity for the job to do the unwinding for however long that takes and to eventually
        /// resolve to notify of a <see cref="CancelComplete"/>.
        /// </summary>
        /// <param name="dataStream">The <see cref="IDriverDataStream{TInstance}"/> to schedule the job on.</param>
        /// <param name="scheduleJobFunction">The callback function to perform the scheduling</param>
        /// <param name="batchStrategy">The <see cref="BatchStrategy"/> to use for scheduling</param>
        /// <typeparam name="TInstance">The type of <see cref="IEntityKeyedTask"/> in the stream</typeparam>
        /// <returns>A <see cref="IJobConfig"/> to allow for chaining more configuration options.</returns>
        public IJobConfig ConfigureJobToCancel<TInstance>(
            IDriverDataStream<TInstance> dataStream,
            JobConfigScheduleDelegates.ScheduleCancelJobDelegate<TInstance> scheduleJobFunction,
            BatchStrategy batchStrategy)
            where TInstance : unmanaged, IEntityKeyedTask
        {
            return TaskSet.ConfigureJobToCancel(
                (EntityProxyDataStream<TInstance>)dataStream,
                scheduleJobFunction,
                batchStrategy);
        }

        /// <summary>
        /// Configures a Job that is triggered by <see cref="Entity"/> or <see cref="IComponentData"/> being
        /// present in the passed in <see cref="EntityQuery"/>.
        /// </summary>
        /// <param name="entityQuery">The <see cref="EntityQuery"/> to trigger the job off of.</param>
        /// <param name="scheduleJobFunction">The scheduling function to call to schedule the job.</param>
        /// <param name="batchStrategy">The <see cref="BatchStrategy"/> to use for executing the job.</param>
        /// <returns>A <see cref="IJobConfig"/> to allow for chaining more configuration options.</returns>
        protected IJobConfig ConfigureJobTriggeredBy(
            EntityQuery entityQuery,
            JobConfigScheduleDelegates.ScheduleEntityQueryJobDelegate scheduleJobFunction,
            BatchStrategy batchStrategy)
        {
            return TaskSet.ConfigureJobTriggeredBy(
                entityQuery,
                scheduleJobFunction,
                batchStrategy);
        }

        //TODO: #73 - Implement other job types

        //*************************************************************************************************************
        // HARDENING
        //*************************************************************************************************************

        internal void Harden()
        {
            Debug_EnsureNotHardened();
            m_IsHardened = true;

            //Drill down so that the lowest Task Driver gets hardened
            foreach (AbstractTaskDriver subTaskDriver in m_SubTaskDrivers)
            {
                subTaskDriver.Harden();
            }

            //Harden our TaskDriverSystem if it hasn't been already
            TaskDriverSystem.Harden();

            //Harden our own TaskSet
            TaskSet.Harden();

            //TODO: #138 - Can we consolidate this into the TaskSet and have TaskSets aware of parenting instead
            m_HasCancellableData = TaskSet.ExplicitCancellationCount > 0
                || TaskDriverSystem.HasCancellableData
                || m_SubTaskDrivers.Any(subtaskDriver => subtaskDriver.m_HasCancellableData);
        }

        internal void AddJobConfigsTo(List<AbstractJobConfig> jobConfigs)
        {
            TaskSet.AddJobConfigsTo(jobConfigs);
        }

        void ITaskSetOwner.AddResolvableDataStreamsTo(Type type, List<AbstractDataStream> dataStreams)
        {
            TaskSet.AddResolvableDataStreamsTo(type, dataStreams);
        }

        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************

        [Conditional("DEBUG")]
        private void DEBUG_EnsureValidAttributes()
        {
            Type type = GetType();

            IEnumerable<string> invalidAttributeNames = type.GetCustomAttributes(true)
                .Where(attribute => attribute is UpdateAfterAttribute or UpdateBeforeAttribute)
                .Select(attribute => attribute.GetType().Name);

            if (!invalidAttributeNames.Any())
            {
                return;
            }

            Logger.Error(
                $"Unsupported Attributes. The following attributes are defined on the task driver but are not supported. (see below)"
                + $"\n{string.Join(", ", invalidAttributeNames)}"
                + $"\nNOTE: If the task driver must be ordered define a custom system type to place attributes on.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureNotHardened()
        {
            if (m_IsHardened)
            {
                throw new InvalidOperationException($"Trying to Harden {this} but {nameof(Harden)} has already been called!");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureHardened()
        {
            if (!m_IsHardened)
            {
                throw new InvalidOperationException($"Expected {this} to be Hardened but it hasn't yet!");
            }
        }
    }
}