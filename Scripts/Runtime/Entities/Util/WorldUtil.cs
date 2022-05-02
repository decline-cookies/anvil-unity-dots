using System;
using System.Collections.Generic;
using System.Linq;
using Anvil.CSharp.Logging;
using Unity.Entities;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// A system group to house a <see cref="World"/>s <see cref="EndInitializationEntityCommandBufferSystem"/>.
    /// Used for mutli-world playerloop optimization.
    /// </summary>
    /// <remarks>Kept outside <see cref="WorldUtil"/> so their names read better in the editor.</remarks>
    [DisableAutoCreation]
    internal class EndInitializationCommandBufferSystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// A system group to house a <see cref="World"/>s <see cref="EndSimulationEntityCommandBufferSystem"/>.
    /// Used for mutli-world playerloop optimization.
    /// </summary>
    /// <remarks>Kept outside <see cref="WorldUtil"/> so their names read better in the editor.</remarks>
    [DisableAutoCreation]
    internal class EndSimulationCommandBufferSystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// A <see cref="PlayerLoop"/> phase inserted immediately after <see cref="Update"/>.
    /// Add via <see cref="WorldUtil.AddCustomPhasesToCurrentPlayerLoop" />.
    /// </summary>
    internal class PostUpdate
    {
        /// <summary>
        /// Provides a <see cref="PlayerLoopSystem" /> instance to to identify this phase.
        /// </summary>
        public static PlayerLoopSystem PlayerLoopSystem
        {
            get => new PlayerLoopSystem()
            {
                type = typeof(PostUpdate)
            };
        }
    }

    /// <summary>
    /// A <see cref="PlayerLoop"/> phase inserted immediately after <see cref="Initialization"/>.
    /// Add via <see cref="WorldUtil.AddCustomPhasesToCurrentPlayerLoop" />.
    /// </summary>
    internal class PostInitialization
    {
        /// <summary>
        /// Provides a <see cref="PlayerLoopSystem" /> instance to to identify this phase.
        /// </summary>
        public static PlayerLoopSystem PlayerLoopSystem
        {
            get => new PlayerLoopSystem()
            {
                type = typeof(PostInitialization)
            };
        }
    }

    /// <summary>
    /// A collection of utilities to manipulate and augments <see cref="World"/>s.
    /// The Anvil compliment to <see cref="ScriptBehaviourUpdateOrder"/>.
    /// </summary>
    public static class WorldUtil
    {
        // No need to reset between play sessions because PlayerLoop systems are stateless and 
        // persist between sessions when domain reloading is disabled.
        private static bool s_AreCustomPlayerLoopPhasesAdded = false;

        /// <summary>
        /// Add custom phases to the <see cref="PlayerLoop"/>.
        /// <see cref="PostInitialization"/> and <see cref="PostUpdate"/>.
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
            Debug.Assert(!topLevelPhases.Any((phase) => phase.type == typeof(PostInitialization)), $"{nameof(PostInitialization)} phase already added");
            topLevelPhases.Insert(initializationPhaseIndex + 1, PostInitialization.PlayerLoopSystem);

            int updatePhaseIndex = topLevelPhases.FindIndex((phase) => phase.type == typeof(Update));
            Debug.Assert(updatePhaseIndex != -1, "Update phase not found");
            Debug.Assert(!topLevelPhases.Any((phase) => phase.type == typeof(PostUpdate)), $"{nameof(PostUpdate)} phase already added");
            topLevelPhases.Insert(updatePhaseIndex + 1, PostUpdate.PlayerLoopSystem);

            playerLoop.subSystemList = topLevelPhases.ToArray();
            PlayerLoop.SetPlayerLoop(playerLoop);

            s_AreCustomPlayerLoopPhasesAdded = true;
        }

        /// <summary>
        /// Create a collection of top level <see cref="ComponentSystemGroup" />s and add them to the current <see cref="PlayerLoop"/>.
        /// </summary>
        /// <param name="world">The world to create top level groups in.</param>
        /// <param name="topLevelGroupTypes">
        /// A collection of value pairs where the first value is the PlayerLoop phase to place the system in and the second value is the 
        /// system to create.
        /// </param>
        /// <returns></returns>
        /// <remarks>
        /// Groups must not already exist in the world. This is a limitation of our ability to detect whether a system has already been 
        /// added to the palyer loop.
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

                ScriptBehaviourUpdateOrder.AppendSystemToPlayerLoopList(topLevelGroups[i], ref playerLoop, playerLoopSystemType);
            }

            PlayerLoop.SetPlayerLoop(playerLoop);

            return topLevelGroups;
        }

        /// <summary>
        /// Sort all groups in a <see cref="World"/>.
        /// </summary>
        public static void SortAllGroupsInWorld(World world)
        {
            // This implementation is a bit overkill since calling ComponentSystemGroup.SortSystems() calls recursively down to subgroups
            // but the way ScriptBehaviourUpdateOrder adds systems to the PlayerLoop prevents us from inspecting instances and any logic 
            // to calculate which systems are top level (check if not a child of other systems) is likely more expensive than just overcalling SortSystems().
            // SortSystems exits early if no sorting is required.
            foreach (ComponentSystemBase system in world.Systems)
            {
                (system as ComponentSystemGroup)?.SortSystems();
            }
        }

        private static readonly (Type PlayerLoopSystemType, Type SystemGroupType)[] s_MultiWorldTopLevelGroupTypes = new[]{
            (typeof(PostInitialization), typeof(EndInitializationCommandBufferSystemGroup)),
            (typeof(PostUpdate), typeof(EndSimulationCommandBufferSystemGroup)),
        };
        /// <summary>
        /// Optimizes a <see cref="World"/>'s <see cref="ComponentSystemGroup"/>s for multi-world applications.
        /// Groups end command buffers into their own <see cref="PlayerLoop"/> phase just after their default phase. 
        /// (Ex: <see cref="Update"/> -> <see cref="PostUpdate"/>)
        /// This allows end command buffers for all worlds to be evaluated after all worlds have scheduled their work for the phase.
        /// The result is that other worlds can compute their jobified work while one world is executing its end command buffer on the main thread.
        /// </summary>
        /// <param name="world">The world instance to optimize.</param>
        /// <remarks>
        /// This method doesn't move <see cref="EndFixedStepSimulationEntityCommandBufferSystem"/> because it's embedded in the <see cref="SimulationSystemGroup"/>.
        /// making it impractical to group with the other worlds. There shouldn't be any expensive work in that command buffer anyway.
        /// </remarks>
        public static void OptimizeWorldForMultiWorldInCurrentPlayerLoop(World world)
        {
            AddCustomPhasesToCurrentPlayerLoop();
            AddTopLevelGroupsToCurrentPlayerLoop(world, s_MultiWorldTopLevelGroupTypes);

            MoveSystemFromToGroup<EndInitializationEntityCommandBufferSystem, InitializationSystemGroup, EndInitializationCommandBufferSystemGroup>(world);
            MoveSystemFromToGroup<EndSimulationEntityCommandBufferSystem, SimulationSystemGroup, EndSimulationCommandBufferSystemGroup>(world);

            // Suppress the logging during sort so we don't see complaints about systems that position themselves based on
            // the systems that we're moving. So far, the configured above are at the end of the group and don't cause issues
            // aside from the warnings. (dependent systems end up in the right place anyway)
            try
            {
#if LOG_VERBOSE
                UnityEngine.Debug.LogWarning($"Sorting {nameof(PlayerLoopSystem)}s. Warning expected with systems trying to position against ${nameof(EndSimulationEntityCommandBufferSystem)}");
#else
                Log.SupressLogging = true;
#endif
                SortAllGroupsInWorld(world);
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                Log.SupressLogging = false;
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