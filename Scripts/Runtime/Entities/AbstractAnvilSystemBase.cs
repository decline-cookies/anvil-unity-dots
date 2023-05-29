using Anvil.CSharp.Logging;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Reflection;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// The base class for Systems when using the Anvil Framework.
    /// Adds some convenience functionality non-release safety checks for
    /// <see cref="SystemBase"/> implementations.
    /// </summary>
    /// <remarks>
    /// To avoid the differences between Systems being created automatically by Unity for the Default World and then
    /// having to be manually registered and added to the update list for other Worlds, all
    /// <see cref="AbstractAnvilSystemBase"/> will try to add themselves to whatever group their
    /// <see cref="UpdateInGroupAttribute"/> points to. This allows for using the same common API of
    /// <see cref="World.GetOrCreateSystem{T}"/> regardless of what <see cref="World"/> the caller is in.
    /// <see cref="OnCreate"/> for implementation details.
    /// </remarks>
    public abstract partial class AbstractAnvilSystemBase : SystemBase
    {
        private Type m_UpdateInGroupType;
        private Logger? m_Logger;

        /// <summary>
        /// Returns a <see cref="Logger"/> for this instance to emit log messages with.
        /// Lazy instantiated.
        /// </summary>
        protected Logger Logger
        {
            get => m_Logger ?? (m_Logger = Log.GetLogger(this, $"({World.Name}) ")).Value;
            set => m_Logger = value;
        }

#if ANVIL_DEBUG_SAFETY
        /// <inheritdoc cref="Dependency" />
        protected new JobHandle Dependency
        {
            get => base.Dependency;
            // Detects situations where the existing dependency is overwritten rather than chained or combined.
            set
            {
                if (!value.DependsOn(base.Dependency))
                {
                    throw new InvalidOperationException($"Dependency Chain Broken: Dependency Chain Broken: The incoming dependency does not contain the existing dependency in the chain.");
                }

                base.Dependency = value;
            }
        }
#endif

        /// <summary>
        /// Creates a new <see cref="AbstractAnvilSystemBase"/> instance.
        /// </summary>
        protected AbstractAnvilSystemBase() : base() { }

        protected override void OnCreate()
        {
            base.OnCreate();

            EnsureSystemIsInUpdateGroup();
        }

        protected override void OnDestroy()
        {
            WorldInternal.OnSystemCreated -= WorldInternal_OnSystemCreated;
            base.OnDestroy();
        }

        private void EnsureSystemIsInUpdateGroup()
        {
            //We could be created for a different world in which case we won't be in the group's update loop.
            //First we try to get the group attribute
            Type type = GetType();
            UpdateInGroupAttribute updateInGroupAttribute = type.GetCustomAttribute<UpdateInGroupAttribute>();
            if (updateInGroupAttribute == null)
            {
                //If nothing, then we don't do anything, the developer is responsible
                return;
            }

            m_UpdateInGroupType = updateInGroupAttribute.GroupType;

            //We'll get or create the system specified in the attribute. It SHOULD be a Group, but might not be so...
            ComponentSystemBase componentSystemBase = World.GetOrCreateSystem(m_UpdateInGroupType);
            if (componentSystemBase is not ComponentSystemGroup componentSystemGroup)
            {
                //We'll rightly complain about it.
                throw new InvalidOperationException($"{type.GetReadableName()} is trying to set its {nameof(UpdateInGroupAttribute)} to use {m_UpdateInGroupType} but that isn't a {typeof(ComponentSystemGroup).GetReadableName()}!");
            }

            //We can't add to the update list until we're created and we can't force a manual create
            if (componentSystemGroup.Created == false)
            {
                WorldInternal.OnSystemCreated += WorldInternal_OnSystemCreated;
            }
            else
            {
                AddSystemToUpdateList(componentSystemGroup);
            }
        }

        private void WorldInternal_OnSystemCreated(World world, ComponentSystemBase createdSystem)
        {
            //If this doesn't concern us...
            if (World != world || createdSystem.GetType() != m_UpdateInGroupType)
            {
                return;
            }
            //Otherwise, we can finish up
            WorldInternal.OnSystemCreated -= WorldInternal_OnSystemCreated;
            AddSystemToUpdateList((ComponentSystemGroup)createdSystem);
        }

        private void AddSystemToUpdateList(ComponentSystemGroup componentSystemGroup)
        {
            //This function early returns if we're already part of the group, like in the case of automatic
            //default world init, so it's safe to just call to ensure we're part of the right update system.
            componentSystemGroup.AddSystemToUpdateList(this);
        }

        //Used often for getting a query from a specific system from the outside so that the
        //query is associated to that system's dependency. Unity already does this but for
        //the API where you pass in an EntityQueryBuilder
        public new EntityQuery GetEntityQuery(params ComponentType[] componentTypes)
        {
            return base.GetEntityQuery(componentTypes);
        }
    }
}