using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Anvil.CSharp.Logging;
using Unity.Entities;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// A system group to house a <see cref="World"/>s <see cref="EndInitializationEntityCommandBufferSystem"/>.
    /// Used for multi-world player loop optimization.
    /// </summary>
    /// <remarks>Kept outside <see cref="WorldUtil"/> so their names read better in the editor.</remarks>
    [DisableAutoCreation]
    internal class EndInitializationCommandBufferSystemGroup_Anvil : ComponentSystemGroup { }

    /// <summary>
    /// A system group to house a <see cref="World"/>s <see cref="EndSimulationEntityCommandBufferSystem"/>.
    /// Used for multi-world player loop optimization.
    /// </summary>
    /// <remarks>Kept outside <see cref="WorldUtil"/> so their names read better in the editor.</remarks>
    [DisableAutoCreation]
    internal class EndSimulationCommandBufferSystemGroup_Anvil : ComponentSystemGroup { }

    /// <summary>
    /// A <see cref="PlayerLoop"/> phase inserted immediately after <see cref="Update"/>.
    /// Add via <see cref="WorldUtil.AddCustomPhasesToCurrentPlayerLoop" />.
    /// </summary>
    internal class PostUpdate_Anvil
    {
        /// <summary>
        /// Provides a <see cref="PlayerLoopSystem" /> instance to to identify this phase.
        /// </summary>
        public static PlayerLoopSystem PlayerLoopSystem
        {
            get => new PlayerLoopSystem()
            {
                type = typeof(PostUpdate_Anvil)
            };
        }
    }

    /// <summary>
    /// A <see cref="PlayerLoop"/> phase inserted immediately after <see cref="Initialization"/>.
    /// Add via <see cref="WorldUtil.AddCustomPhasesToCurrentPlayerLoop" />.
    /// </summary>
    internal class PostInitialization_Anvil
    {
        /// <summary>
        /// Provides a <see cref="PlayerLoopSystem" /> instance to to identify this phase.
        /// </summary>
        public static PlayerLoopSystem PlayerLoopSystem
        {
            get => new PlayerLoopSystem()
            {
                type = typeof(PostInitialization_Anvil)
            };
        }
    }

    /// <summary>
    /// A collection of utilities to manipulate and augments <see cref="World"/>s.
    /// The Anvil compliment to <see cref="ScriptBehaviourUpdateOrder"/>.
    /// </summary>
    public static class WorldUtil
    {
        private static readonly EventInfo s_WorldDestroyedEvent = typeof(World).GetEvent("WorldDestroyed", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly EventInfo s_WorldCreatedEvent = typeof(World).GetEvent("WorldCreated", BindingFlags.Static | BindingFlags.NonPublic);

        /// <summary>
        /// Dispatched before a world is destroyed.
        /// </summary>
        /// <remarks>
        /// This is a proxy for the internal <see cref="World.WorldDestroyed"/> static event that Unity has made internal.
        /// If Unity makes the event public or provides a mechanism for a world instance to know when it is being disposed
        /// this method will be redundant.
        /// </remarks>
        //TODO: Use assembly injection?
        public static event Action<World> OnWorldDestroyed
        {
            add => s_WorldDestroyedEvent.AddMethod.Invoke(null, new[] { value });
            remove => s_WorldDestroyedEvent.RemoveMethod.Invoke(null, new[] { value });
        }

        /// <summary>
        /// Dispatched after a world is created.
        /// </summary>
        /// <remarks>
        /// This is a proxy for the internal <see cref="World.WorldCreated"/> static event that Unity has made internal.
        /// Added for the sake of completion after adding <see cref="OnWorldDestroyed"/>.
        /// </remarks>
        public static event Action<World> OnWorldCreated
        {
            add => s_WorldCreatedEvent.AddMethod.Invoke(null, new[] { value });
            remove => s_WorldCreatedEvent.RemoveMethod.Invoke(null, new[] { value });
        }

        // No need to reset between play sessions because PlayerLoop systems are stateless and
        // persist between sessions when domain reloading is disabled.
        private static bool s_AreCustomPlayerLoopPhasesAdded = false;

        static WorldUtil()
        {
            Debug.Assert(s_WorldDestroyedEvent != null);
            Debug.Assert(s_WorldDestroyedEvent.EventHandlerType == typeof(Action<World>));

            Debug.Assert(s_WorldCreatedEvent != null);
            Debug.Assert(s_WorldCreatedEvent.EventHandlerType == typeof(Action<World>));
        }

        /// <summary>
        /// Add custom phases to the <see cref="PlayerLoop"/>.
        /// <see cref="PostInitialization_Anvil"/> and <see cref="PostUpdate_Anvil"/>.
        /// </summary>
        public static void AddCustomPhasesToCurrentPlayerLoop()
        {
            if (s_AreCustomPlayerLoopPhasesAdded)
            {
                return;
            }

            // Create new top level player loop phases
            PlayerLoopSystem playerLoop = PlayerLoop.GetCurrentPlayerLoop();
            List<PlayerLoopSystem> topLevelPhases = playerLoop.subSystemList.ToList();

            int initializationPhaseIndex = topLevelPhases.FindIndex((phase) => phase.type == typeof(Initialization));
            Debug.Assert(initializationPhaseIndex != -1, $"{nameof(Initialization)} phase not found");
            Debug.Assert(!topLevelPhases.Any((phase) => phase.type == typeof(PostInitialization_Anvil)), $"{nameof(PostInitialization_Anvil)} phase already added");
            topLevelPhases.Insert(initializationPhaseIndex + 1, PostInitialization_Anvil.PlayerLoopSystem);

            int updatePhaseIndex = topLevelPhases.FindIndex((phase) => phase.type == typeof(Update));
            Debug.Assert(updatePhaseIndex != -1, "Update phase not found");
            Debug.Assert(!topLevelPhases.Any((phase) => phase.type == typeof(PostUpdate_Anvil)), $"{nameof(PostUpdate_Anvil)} phase already added");
            topLevelPhases.Insert(updatePhaseIndex + 1, PostUpdate_Anvil.PlayerLoopSystem);

            playerLoop.subSystemList = topLevelPhases.ToArray();
            PlayerLoop.SetPlayerLoop(playerLoop);

            s_AreCustomPlayerLoopPhasesAdded = true;
        }

        /// <summary>
        /// Create a collection of top level <see cref="ComponentSystemGroup" />s and add them to the current
        /// <see cref="PlayerLoop"/>.
        /// </summary>
        /// <param name="world">The world to create top level groups in.</param>
        /// <param name="topLevelGroupTypes">
        /// A collection of value pairs where the first value is the PlayerLoop phase to place the system in and the
        /// second value is the system to create.
        /// </param>
        /// <returns>A collection of the system groups created.</returns>
        /// <remarks>
        /// Groups must not already exist in the world. This is a limitation of our ability to detect whether a system
        /// has already been added to the player loop.
        /// </remarks>
        public static ComponentSystemGroup[] AddTopLevelGroupsToCurrentPlayerLoop(World world, (Type PlayerLoopSystemType, Type SystemGroupType)[] topLevelGroupTypes)
        {
            PlayerLoopSystem playerLoop = PlayerLoop.GetCurrentPlayerLoop();

            // Create and Add top level system groups to the player loop
            ComponentSystemGroup[] topLevelGroups = new ComponentSystemGroup[topLevelGroupTypes.Length];
            for (int i = 0; i < topLevelGroupTypes.Length; i++)
            {
                (Type playerLoopSystemType, Type systemGroupType) = topLevelGroupTypes[i];
                Debug.Assert(systemGroupType.IsSubclassOf(typeof(ComponentSystemGroup)));

                // If we had access to ScriptBehaviourUpdateOrder.DummyDelegateWrapper we could detect if the system has already been added to the PlayerLoop.
                Debug.Assert(world.GetExistingSystem(systemGroupType) == null, $"System group cannot already exist in world {world.Name}. There is no way of knowing if a top level group for an individual world has been added to the player loop already.");
                topLevelGroups[i] = (ComponentSystemGroup)world.CreateSystem(systemGroupType);

                ScriptBehaviourUpdateOrder.AppendSystemToPlayerLoop(topLevelGroups[i], ref playerLoop, playerLoopSystemType);
            }

            PlayerLoop.SetPlayerLoop(playerLoop);

            return topLevelGroups;
        }

        /// <summary>
        /// Sort all groups in a <see cref="World"/>.
        /// </summary>
        public static void SortAllGroupsInWorld(World world)
        {
            // This implementation is a bit overkill since calling ComponentSystemGroup.SortSystems() calls recursively
            // down to subgroups but the way ScriptBehaviourUpdateOrder adds systems to the PlayerLoop prevents us from
            // inspecting instances and any logic to calculate which systems are top level (check if not a child of
            // other systems) is likely more expensive than just over calling SortSystems(). SortSystems exits early if
            // no sorting is required.
            foreach (ComponentSystemBase system in world.Systems)
            {
                (system as ComponentSystemGroup)?.SortSystems();
            }
        }

        private static readonly (Type PlayerLoopSystemType, Type SystemGroupType)[] s_MultiWorldTopLevelGroupTypes = new[]{
            (typeof(PostInitialization_Anvil), typeof(EndInitializationCommandBufferSystemGroup_Anvil)),
            (typeof(PostUpdate_Anvil), typeof(EndSimulationCommandBufferSystemGroup_Anvil)),
        };
        /// <summary>
        /// Optimizes a <see cref="World"/>'s <see cref="ComponentSystemGroup"/>s for multi-world applications.
        /// Groups end command buffers into their own <see cref="PlayerLoop"/> phase just after their default phase.
        /// (Ex: <see cref="Update"/> -> <see cref="PostUpdate_Anvil"/>)
        /// This allows end command buffers for all worlds to be evaluated after all worlds have scheduled their work
        /// for the phase. The result is that other worlds can compute their jobified work while one world is executing
        /// its end command buffer on the main thread.
        /// </summary>
        /// <param name="world">The world instance to optimize.</param>
        /// <remarks>
        /// This method doesn't move <see cref="EndFixedStepSimulationEntityCommandBufferSystem"/> because it's embedded
        /// in the <see cref="SimulationSystemGroup"/>; making it impractical to group with the other worlds. There
        /// shouldn't be any expensive work in that command buffer anyway.
        /// </remarks>
        public static void OptimizeWorldForMultiWorldInCurrentPlayerLoop(World world)
        {
            AddCustomPhasesToCurrentPlayerLoop();
            AddTopLevelGroupsToCurrentPlayerLoop(world, s_MultiWorldTopLevelGroupTypes);

            MoveSystemFromToGroup<EndInitializationEntityCommandBufferSystem, InitializationSystemGroup, EndInitializationCommandBufferSystemGroup_Anvil>(world);
            MoveSystemFromToGroup<EndSimulationEntityCommandBufferSystem, SimulationSystemGroup, EndSimulationCommandBufferSystemGroup_Anvil>(world);

            // Suppress the logging during sort so we don't see complaints about systems that position themselves based on
            // the systems that we're moving. So far, the configured above are at the end of the group and don't cause issues
            // aside from the warnings. (dependent systems end up in the right place anyway)
            try
            {
#if LOG_VERBOSE
                UnityEngine.Debug.LogWarning($"Sorting {nameof(PlayerLoopSystem)}s. Warning expected with systems trying to position against ${nameof(EndSimulationEntityCommandBufferSystem)}");
#else
                Log.SuppressLogging = true;
#endif
                SortAllGroupsInWorld(world);
            }
            finally
            {
                Log.SuppressLogging = false;
            }
        }

        /// <summary>
        /// Moves a system from one group to another.
        /// </summary>
        /// <typeparam name="System">The system type to move</typeparam>
        /// <typeparam name="SrcGroup">The system's existing group.</typeparam>
        /// <typeparam name="DestGroup">The system's destination group.</typeparam>
        /// <param name="world">The world instance to perform the move in.</param>
        public static void MoveSystemFromToGroup<System, SrcGroup, DestGroup>(World world)
                where System : ComponentSystemBase
                where SrcGroup : ComponentSystemGroup
                where DestGroup : ComponentSystemGroup
        {

            Debug.Assert(typeof(SrcGroup) != typeof(DestGroup), "Source and destination groups should not be the same.");

            ComponentSystemBase system = world.GetExistingSystem<System>();
            ComponentSystemGroup srcGroup = world.GetExistingSystem<SrcGroup>();
            ComponentSystemGroup destGroup = world.GetExistingSystem<DestGroup>();

            // Skip if there are missing elements
            if (system == null || srcGroup == null || destGroup == null)
            {
                throw new ArgumentNullException($"{nameof(MoveSystemFromToGroup)}: One or more of the provided system and groups do not exist. {nameof(System)}:{system}, {nameof(SrcGroup)}:{srcGroup}, {nameof(DestGroup)}:{destGroup}");
            }

            Debug.Assert(srcGroup.Systems.Contains(system), $"{system} is not part of the source group: {srcGroup}");
            srcGroup.RemoveSystemFromUpdateList(system);

            destGroup.AddSystemToUpdateList(system);
        }
    }
}