using Anvil.CSharp.Data;
using Anvil.CSharp.Logging;
using System.Collections.Generic;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal abstract partial class AbstractTaskDriverSystem : AbstractAnvilSystemBase,
                                                               ITaskSetOwner
    {
        private readonly IDProvider m_TaskDriverIDProvider;
        private readonly List<AbstractTaskDriver> m_TaskDrivers;


        public AbstractTaskDriverSystem TaskDriverSystem { get => this; }
        
        public new World World { get; }
        public TaskSet TaskSet { get; }
        public uint ID { get; }


        protected AbstractTaskDriverSystem(World world)
        {
            World = world;

            m_TaskDriverIDProvider = new IDProvider();
            m_TaskDrivers = new List<AbstractTaskDriver>();

            ID = m_TaskDriverIDProvider.GetNextID();

            TaskSet = new TaskSet(this);
        }

        protected override void OnDestroy()
        {
            m_TaskDriverIDProvider.Dispose();
            //We don't own the TaskDrivers registered here, so we won't dispose them
            m_TaskDrivers.Clear();

            TaskSet.Dispose();

            base.OnDestroy();
        }

        public override string ToString()
        {
            return GetType().GetReadableName();
        }

        public uint RegisterTaskDriver(AbstractTaskDriver taskDriver)
        {
            uint taskDriverID = m_TaskDriverIDProvider.GetNextID();
            m_TaskDrivers.Add(taskDriver);
            return taskDriverID;
        }

        public ISystemDataStream<TInstance> GetOrCreateDataStream<TInstance>(CancelBehaviour cancelBehaviour = CancelBehaviour.Default)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            return TaskSet.GetOrCreateDataStream<TInstance>(cancelBehaviour);
        }

        protected override void OnUpdate()
        {
            //TODO: Implement
            // Dependency = CommonTaskSet.Update(Dependency);
        }
    }
}
