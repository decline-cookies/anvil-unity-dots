using System;
using Unity.Entities;
using UnityEngine.LowLevel;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// Helper class for exposing and dealing with some <see cref="PlayerLoopSystem"/> internals.
    /// </summary>
    /// <remarks>
    /// To help reduce ambiguity the names in this class follow a convention.
    ///  - PlayerLoopSystem - Refers to the system itself and not its recursive subsystems
    ///  - PlayerLoop - Refers to the system and all of its subsystems recursively.
    ///
    /// Ex:
    ///  - IsInPlayerLoop would check the provided system, all subsystems and their subsystems.
    ///  - IsInPlayerLoopSystem would check just the provided system.
    /// </remarks>
    public static class PlayerLoopUtil
    {
        public static readonly PlayerLoopSystem NO_PLAYER_LOOP = default;

        /// <summary>
        /// Checks if a <see cref="PlayerLoopSystem"/> is part of a given <see cref="World"/>
        /// </summary>
        /// <remarks>
        /// This is very similar to <see cref="ScriptBehaviourUpdateOrder.IsWorldInPlayerLoop"/> except
        /// that it pertains to just the instance of <see cref="PlayerLoopSystem"/> passed in and not
        /// the children of it. This function is useful for building a mapping of <see cref="PlayerLoopSystem"/>'s
        /// to worlds where as <see cref="ScriptBehaviourUpdateOrder.IsWorldInPlayerLoop"/> is useful for
        /// seeing if a world has been added to the Player Loop in general and is usually used for the top level
        /// <see cref="PlayerLoopSystem"/> from <see cref="PlayerLoop.GetCurrentPlayerLoop"/>.
        /// </remarks>
        /// <param name="playerLoopSystem">The <see cref="PlayerLoopSystem"/> to check.</param>
        /// <param name="world">The <see cref="World"/> that may contain the <see cref="PlayerLoopSystem"/></param>
        /// <returns>
        /// True if part of the world.
        /// False if not part of the world.
        /// False if the <see cref="PlayerLoopSystem"/> is a phase instead of a <see cref="ComponentSystemGroup"/>
        /// </returns>
        public static bool IsPlayerLoopSystemPartOfWorld(ref PlayerLoopSystem playerLoopSystem, World world)
        {
            return TryGetSystemFromPlayerLoopSystem(ref playerLoopSystem, out ComponentSystemBase system)
                && system.World == world;
        }

        /// <summary>
        /// Recursively search the <see cref="PlayerLoopSystem"/> for the instance of a <see cref="ComponentSystem"/>.
        /// </summary>
        /// <param name="system">The <see cref="ComponentSystem"/> to search for.</param>
        /// <param name="playerLoop">
        /// The <see cref="PlayerLoopSystem"/> to start at. This instance and all sub systems are recursively searched
        /// </param>
        /// <returns>True if an instance of the system already exists in the player loop.</returns>
        /// <remarks>
        /// This is a compliment to <see cref="ScriptBehaviourUpdateOrder.IsInPlayerLoop"/> and
        /// <see cref="ScriptBehaviourUpdateOrder.IsWorldInPlayerLoop"/> that is required because
        /// <see cref="ScriptBehaviourUpdateOrder.AppendSystemToPlayerLoop"/> wraps the update call in a dummy class to work
        /// around a limitation with Mono (<see cref="ScriptBehaviourUpdateOrder.DummyDelegateWrapper"/>).
        ///
        /// The behaviour differs slightly from the built in methods where <see cref="playerLoop"/> evaluated to check for
        /// <see cref="system"/> rather than just its sub systems.
        /// </remarks>
        public static bool IsInPlayerLoop(ComponentSystemBase system, ref PlayerLoopSystem playerLoop)
        {
            // Is the system in one of the systems at this level?
            if (IsSubsystemOfPlayerLoopSystem(system, ref playerLoop))
            {
                return true;
            }

            // Recursively check each subsystem's subsystems the system.
            for (int i = 0; i < playerLoop.subSystemList.Length; i++)
            {
                PlayerLoopSystem playerLoopSubSystem = playerLoop.subSystemList[i];
                if (IsInPlayerLoop(system, ref playerLoopSubSystem))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Identify whether the instance of a <see cref="ComponentSystem"/> exists in a subsystem of a
        /// <see cref="PlayerLoopSystem"/>.
        /// (non-recursive)
        /// </summary>
        /// <param name="system">The <see cref="ComponentSystem"/> to search for.</param>
        /// <param name="playerLoopSystem">The <see cref="PlayerLoopSystem"/> to search the subsystems of.</param>
        /// <returns>True if an instance of the system exists in this player loop.</returns>
        public static bool IsSubsystemOfPlayerLoopSystem(ComponentSystemBase system, ref PlayerLoopSystem playerLoopSystem)
        {
            for (int i = 0; i < playerLoopSystem.subSystemList.Length; i++)
            {
                PlayerLoopSystem playerLoopSubSystem = playerLoopSystem.subSystemList[i];
                if (!typeof(ComponentSystemBase).IsAssignableFrom(playerLoopSubSystem.type))
                {
                    continue;
                }

                if (TryGetSystemFromPlayerLoopSystem(ref playerLoopSubSystem, out ComponentSystemBase subSystem) && subSystem == system)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Given a <see cref="PlayerLoopSystem"/>, try to get the associated <see cref="ComponentSystemBase"/>.
        /// (non-recursive)
        /// </summary>
        /// <param name="playerLoopSystem">The <see cref="PlayerLoopSystem"/> to use.</param>
        /// <param name="system">The <see cref="ComponentSystemBase"/> associated.</param>
        /// <returns>
        /// True if there is a system.
        /// False if the provided player loop does not represent a system.
        /// </returns>
        /// <remarks>
        /// Currently proxy's <see cref="ScriptBehaviourUpdateOrderInternal.TryGetSystemFromPlayerLoopSystem"/> but business
        /// logic will move to this method when Unity stops wrapping systems in the DummyDelegateWrapper.
        /// </remarks>
        public static bool TryGetSystemFromPlayerLoopSystem(ref PlayerLoopSystem playerLoopSystem, out ComponentSystemBase system)
            => ScriptBehaviourUpdateOrderInternal.TryGetSystemFromPlayerLoopSystem(ref playerLoopSystem, out system);

        /// <summary>
        /// Recursively search a <see cref="PlayerLoopSystem"/> for an instance of <see cref="playerLoopSystemType"/>.
        /// </summary>
        /// <param name="playerLoop">
        /// The <see cref="PlayerLoopSystem"/> to start at. This instance and all sub systems are recursively searched
        /// </param>
        /// <param name="playerLoopSystemType">The type of the <see cref="PlayerLoopSystem"/> to look for.</param>
        /// <param name="playerLoopSystem">
        /// If found, the instance of the <see cref="PlayerLoopSystem"/> where the <see cref="PlayerLoopSystem.type"/>
        /// matches <see cref="playerLoopSystemType"/>
        /// If no instance is found the value is <see cref="NO_PLAYER_LOOP"/>.
        /// </param>
        /// <returns>True if an instance is found.</returns>
        public static bool TryFindPlayerLoopSystemByType(ref PlayerLoopSystem playerLoop, Type playerLoopSystemType, out PlayerLoopSystem playerLoopSystem)
        {
            if (playerLoop.type == playerLoopSystemType)
            {
                playerLoopSystem = playerLoop;
                return true;
            }

            for (int i = 0; i < playerLoop.subSystemList?.Length; i++)
            {
                ref PlayerLoopSystem playerLoopSubSystem = ref playerLoop.subSystemList[i];
                if (TryFindPlayerLoopSystemByType(ref playerLoopSubSystem, playerLoopSystemType, out playerLoopSystem))
                {
                    return true;
                }
            }

            playerLoopSystem = NO_PLAYER_LOOP;
            return false;
        }
    }
}