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
        private Logger? m_Logger;
        /// <summary>
        /// Returns a <see cref="Logger"/> for this instance to emit log messages with.
        /// Lazy instantiated.
        /// </summary>
        protected Logger Logger
        {
            get => m_Logger ?? (m_Logger = Log.GetLogger(this)).Value;
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
            
            //We'll get or create the system specified in the attribute. It SHOULD be a Group, but might not be so...
            ComponentSystemBase componentSystemBase = World.GetOrCreateSystem(updateInGroupAttribute.GroupType);
            if (componentSystemBase is not ComponentSystemGroup componentSystemGroup)
            {
                //We'll rightly complain about it.
                throw new InvalidOperationException($"{type.GetReadableName()} is trying to set its {nameof(UpdateInGroupAttribute)} to use {updateInGroupAttribute.GroupType} but that isn't a {typeof(ComponentSystemGroup).GetReadableName()}!");
            }
            //This function early returns if we're already part of the group, like in the case of automatic 
            //default world init, so it's safe to just call to ensure we're part of the right update system.
            componentSystemGroup.AddSystemToUpdateList(this);
        }
    }
}