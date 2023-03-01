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
    public class PostUpdate_Anvil
    {
        /// <summary>
        /// Provides a <see cref="PlayerLoopSystem" /> instance to to identify this phase.
        /// </summary>
        internal static PlayerLoopSystem PlayerLoopSystem
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
    public class PostInitialization_Anvil
    {
        /// <summary>
        /// Provides a <see cref="PlayerLoopSystem" /> instance to to identify this phase.
        /// </summary>
        internal static PlayerLoopSystem PlayerLoopSystem
        {
            get => new PlayerLoopSystem()
            {
                type = typeof(PostInitialization_Anvil)
            };
        }
    }

    /// <summary>
    /// A collection of utilities to manipulate and augment <see cref="World"/>s.
    /// The Anvil compliment to <see cref="ScriptBehaviourUpdateOrder"/>.
    /// </summary>
    public static class WorldUtil
    {
        /// <summary>
        /// Default top level <see cref="ComponentSystemGroup"/>s that are added by
        /// <see cref="DefaultWorldInitialization.Initialize"/>.
        /// </summary>
        /// <remarks>
        /// Useful when setting up custom <see cref="World"/> instances.
        /// </remarks>
        public static readonly (Type PlayerLoopSystemType, Type SystemGroupType)[] DEFAULT_TOP_LEVEL_GROUPS = new[]
        {
            (typeof(Initialization), typeof(InitializationSystemGroup)),
            (typeof(Update), typeof(SimulationSystemGroup)),
            (typeof(PreLateUpdate), typeof(PresentationSystemGroup))
        };

        // No need to reset between play sessions because PlayerLoop systems are stateless and
        // persist between sessions when domain reloading is disabled.
        private static bool s_AreCustomPlayerLoopPhasesAdded = false;

        static WorldUtil()
        {
            Debug.Assert(
                DEFAULT_TOP_LEVEL_GROUPS.All((groupTypeDef) => groupTypeDef.SystemGroupType.IsSubclassOf(typeof(ComponentSystemGroup))),
                "Top level system groups must be ComponentSystemGroups");
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
            Debug.Assert(
                !topLevelPhases.Any((phase) => phase.type == typeof(PostInitialization_Anvil)),
                $"{nameof(PostInitialization_Anvil)} phase already added");
            topLevelPhases.Insert(initializationPhaseIndex + 1, PostInitialization_Anvil.PlayerLoopSystem);

            int updatePhaseIndex = topLevelPhases.FindIndex((phase) => phase.type == typeof(Update));
            Debug.Assert(updatePhaseIndex != -1, "Update phase not found");
            Debug.Assert(
                !topLevelPhases.Any((phase) => phase.type == typeof(PostUpdate_Anvil)),
                $"{nameof(PostUpdate_Anvil)} phase already added");
            topLevelPhases.Insert(updatePhaseIndex + 1, PostUpdate_Anvil.PlayerLoopSystem);

            playerLoop.subSystemList = topLevelPhases.ToArray();
            PlayerLoop.SetPlayerLoop(playerLoop);

            s_AreCustomPlayerLoopPhasesAdded = true;
        }

        /// <summary>
        /// Create or Migrate a collection of <see cref="ComponentSystemGroup" />s to be top level groups added as
        /// subsystems to existing <see cref="PlayerLoopSystem"/>s in the current <see cref="PlayerLoop"/>.
        ///
        /// If disposing a world that this has been called on make sure to call
        /// <see cref="ScriptBehaviourUpdateOrder.RemoveWorldFromCurrentPlayerLoop" /> to remove the top level groups
        /// from the player loop.
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
        public static ComponentSystemGroup[] SetupTopLevelGroupsInCurrentPlayerLoop(
            World world,
            (Type PlayerLoopSystemType, Type SystemGroupType)[] topLevelGroupTypes)
        {
            PlayerLoopSystem playerLoop = PlayerLoop.GetCurrentPlayerLoop();

            // Create and Add top level system groups to the player loop
            ComponentSystemGroup[] topLevelGroups = new ComponentSystemGroup[topLevelGroupTypes.Length];
            for (int i = 0; i < topLevelGroupTypes.Length; i++)
            {
                (Type playerLoopSystemType, Type systemGroupType) = topLevelGroupTypes[i];
                Debug.Assert(systemGroupType.IsSubclassOf(typeof(ComponentSystemGroup)));

                ComponentSystemGroup group = world.GetExistingSystem(systemGroupType) as ComponentSystemGroup;
                if (group == null)
                {
                    group = world.CreateSystem(systemGroupType) as ComponentSystemGroup;
                    ScriptBehaviourUpdateOrder.AppendSystemToPlayerLoop(group, ref playerLoop, playerLoopSystemType);
                }
                else
                {
                    MigrateExistingGroupToPlayerLoop(group, ref playerLoop, playerLoopSystemType);
                }

                topLevelGroups[i] = group;
            }

            PlayerLoop.SetPlayerLoop(playerLoop);

            return topLevelGroups;
        }

        private static void MigrateExistingGroupToPlayerLoop(ComponentSystemGroup group, ref PlayerLoopSystem playerLoop, Type playerLoopSystemType)
        {
            // If the group already has a parent group remove it from the parent.
            if (group.TryFindParentGroup(out ComponentSystemGroup parentGroup))
            {
                parentGroup.RemoveSystemFromUpdateList(group);
                parentGroup.SortSystems();
            }

            // In the unlikely event that the system existed in multiple groups warn the user. This should
            // never be the case for a group that is intended to be top level.
            Debug.Assert(
                !group.TryFindParentGroup(out _),
                $"Top level group exists in multiple parent groups. "
                + $"Group:{group.GetType().GetReadableName()}, World:{group.World.Name}");

            // If an instance of the group already exists at the player loop location then skip.
            // We don't want to add it twice.
            if (PlayerLoopUtil.TryFindPlayerLoopSystemByType(ref playerLoop, playerLoopSystemType, out PlayerLoopSystem playerLoopSystem)
                && PlayerLoopUtil.IsSubsystemOfPlayerLoopSystem(group, ref playerLoopSystem))
            {
                // Emit an error because this is probably a symptom of a larger problem but we can avoid
                // the unintended behaviour of a system instance getting updated multiple times.
                Log.GetStaticLogger(typeof(WorldUtil))
                    .Error(
                        $"Top level group already exists for this world at the requested player loop phase. "
                        + $"Group:{group.GetType().GetReadableName()}, World:{group.World.Name}");
            }
            else
            {
                ScriptBehaviourUpdateOrder.AppendSystemToPlayerLoop(group, ref playerLoop, playerLoopSystemType);
            }
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

        private static readonly (Type PlayerLoopSystemType, Type SystemGroupType)[] s_MultiWorldTopLevelGroupTypes = new[]
        {
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
            SetupTopLevelGroupsInCurrentPlayerLoop(world, s_MultiWorldTopLevelGroupTypes);

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