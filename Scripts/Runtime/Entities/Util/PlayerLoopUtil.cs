using System;
using System.Reflection;
using Unity.Entities;
using UnityEngine.LowLevel;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// Helper class for exposing and dealing with some <see cref="PlayerLoopSystem"/> internals.
    /// </summary>
    public static class PlayerLoopUtil
    {
        private static readonly Type COMPONENT_SYSTEM_GROUP_TYPE = typeof(ComponentSystemGroup);
        private static readonly MethodInfo s_ScriptBehaviourUpdateOrder_IsDelegateForWorldSystem_MethodInfo = typeof(ScriptBehaviourUpdateOrder).GetMethod("IsDelegateForWorldSystem", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly IsDelegateForWorldSystemDelegate s_IsDelegateForWorldSystem = (IsDelegateForWorldSystemDelegate)Delegate.CreateDelegate(typeof(IsDelegateForWorldSystemDelegate), s_ScriptBehaviourUpdateOrder_IsDelegateForWorldSystem_MethodInfo);
        private static MethodInfo s_DummyDelegateWrapper_System;

        private delegate bool IsDelegateForWorldSystemDelegate(World world, ref PlayerLoopSystem playerLoopSystem);

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
            return playerLoopSystem.updateDelegate?.Target != null && s_IsDelegateForWorldSystem(world, ref playerLoopSystem);
        }

        /// <summary>
        /// Given a <see cref="PlayerLoopSystem"/>, gets the <see cref="ComponentSystemGroup"/> associated.
        /// This is a "safe" method, it does not assume there is a <see cref="ComponentSystemGroup"/> to get
        /// and may return null.
        /// </summary>
        /// <param name="playerLoopSystem">The <see cref="PlayerLoopSystem"/> to use.</param>
        /// <param name="systemGroup">The <see cref="ComponentSystemGroup"/> if it exists.</param>
        /// <returns>
        /// True if a system group exists
        /// False if this <see cref="PlayerLoopSystem"/> is a phase
        /// False if this <see cref="PlayerLoopSystem"/> does not have a <see cref="ComponentSystemGroup"/>
        /// </returns>
        public static bool TryGetSystemGroupFromPlayerLoopSystem(ref PlayerLoopSystem playerLoopSystem, out ComponentSystemGroup systemGroup)
        {
            systemGroup = null;
            if (playerLoopSystem.updateDelegate?.Target == null
             || !COMPONENT_SYSTEM_GROUP_TYPE.IsAssignableFrom(playerLoopSystem.type))
            {
                return false;
            }

            return TryGetSystemGroupFromPlayerLoopSystemNoChecks(ref playerLoopSystem, out systemGroup);
        }

        /// <summary>
        /// Given a <see cref="PlayerLoopSystem"/>, gets the <see cref="ComponentSystemGroup"/> associated.
        /// This method has no checks and assumes that you are certain the <see cref="PlayerLoopSystem"/>
        /// does contain a <see cref="ComponentSystemGroup"/>. 
        /// </summary>
        /// <param name="playerLoopSystem">The <see cref="PlayerLoopSystem"/> to use.</param>
        /// <param name="systemGroup">The <see cref="ComponentSystemGroup"/> associated.</param>
        /// <returns>
        /// True if there is a system group
        /// False if not
        /// </returns>
        public static bool TryGetSystemGroupFromPlayerLoopSystemNoChecks(ref PlayerLoopSystem playerLoopSystem, out ComponentSystemGroup systemGroup)
        {
            bool result = TryGetSystemFromPlayerLoopSystemNoChecks(ref playerLoopSystem, out ComponentSystemBase system);
            systemGroup = (ComponentSystemGroup)system;
            return result;
        }

        /// <summary>
        /// Given a <see cref="PlayerLoopSystem"/>, gets the <see cref="ComponentSystemBase"/> associated.
        /// This is a "safe" method, it does not assume there is a <see cref="ComponentSystemBase"/> to get
        /// and may return null.
        /// </summary>
        /// <param name="playerLoopSystem">The <see cref="PlayerLoopSystem"/> to use.</param>
        /// <param name="system">The <see cref="ComponentSystemBase"/> if it exists.</param>
        /// <returns>
        /// True if there is a system
        /// False if this <see cref="PlayerLoopSystem"/> is a phase or if this <see cref="PlayerLoopSystem"/> does
        /// not have a <see cref="ComponentSystemBase"/>
        /// </returns>
        public static bool TryGetSystemFromPlayerLoopSystem(ref PlayerLoopSystem playerLoopSystem, out ComponentSystemBase system)
        {
            system = null;
            return playerLoopSystem.updateDelegate?.Target != null && TryGetSystemFromPlayerLoopSystemNoChecks(ref playerLoopSystem, out system);
        }

        /// <summary>
        /// Given a <see cref="PlayerLoopSystem"/>, gets the <see cref="ComponentSystemBase"/> associated.
        /// This method has no checks and assumes that you are certain the <see cref="PlayerLoopSystem"/>
        /// does contain a <see cref="ComponentSystemBase"/>. 
        /// </summary>
        /// <param name="playerLoopSystem">The <see cref="PlayerLoopSystem"/> to use.</param>
        /// <param name="system">The <see cref="ComponentSystemBase"/> associated.</param>
        /// <returns>
        /// True if there is a system
        /// False if not
        /// </returns>
        public static bool TryGetSystemFromPlayerLoopSystemNoChecks(ref PlayerLoopSystem playerLoopSystem, out ComponentSystemBase system)
        {
            object wrapper = playerLoopSystem.updateDelegate.Target;

            //We have to lazy create this because we don't have access to the wrapper's type until we see it at runtime.
            if (s_DummyDelegateWrapper_System == null)
            {
                s_DummyDelegateWrapper_System = wrapper.GetType().GetProperty("System", BindingFlags.Instance | BindingFlags.NonPublic).GetMethod;
            }

            //TODO: #28 We can speed the reflected call up by using Expression Trees to create a strongly typed delegate
            //We should be able to call with the params we have access to and pass in "object" for the param we don't.
            //In the expression lambda, we do a convert to the internal type defined on the method info.
            system = (ComponentSystemBase)s_DummyDelegateWrapper_System.Invoke(wrapper, null);

            return system != null;
        }
    }
}
