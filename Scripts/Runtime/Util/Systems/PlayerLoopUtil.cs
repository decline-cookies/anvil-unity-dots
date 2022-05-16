using System;
using System.Reflection;
using Unity.Entities;
using UnityEngine.LowLevel;

namespace Anvil.Unity.DOTS.Util
{
    /// <summary>
    /// Helper class for exposing and dealing with some <see cref="PlayerLoopSystem"/> internals.
    /// </summary>
    public static class PlayerLoopUtil
    {
        private static readonly Type COMPONENT_SYSTEM_GROUP_TYPE = typeof(ComponentSystemGroup);
        private static readonly MethodInfo s_ScriptBehaviourUpdateOrder_IsDelegateForWorldSystem_MethodInfo = typeof(ScriptBehaviourUpdateOrder).GetMethod("IsDelegateForWorldSystem", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly IsDelegateForWorldSystemDelegate s_IsDelegateForWorldSystem = (IsDelegateForWorldSystemDelegate)Delegate.CreateDelegate(typeof(IsDelegateForWorldSystemDelegate), s_ScriptBehaviourUpdateOrder_IsDelegateForWorldSystem_MethodInfo);
        private static PropertyInfo s_DummyDelegateWrapper_System;
        
        private delegate bool IsDelegateForWorldSystemDelegate(World world, ref PlayerLoopSystem playerLoopSystem);
        
        /// <summary>
        /// Checks if a <see cref="PlayerLoopSystem"/> is part of a given <see cref="World"/>
        /// </summary>
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
        /// <returns>
        /// The <see cref="ComponentSystemGroup"/> if it exists.
        /// null if this <see cref="PlayerLoopSystem"/> is a phase
        /// null if this <see cref="PlayerLoopSystem"/> does not have a <see cref="ComponentSystemGroup"/>
        /// </returns>
        public static ComponentSystemGroup GetSystemGroupFromPlayerLoopSystem(ref PlayerLoopSystem playerLoopSystem)
        {
            if (playerLoopSystem.updateDelegate?.Target == null
             || !COMPONENT_SYSTEM_GROUP_TYPE.IsAssignableFrom(playerLoopSystem.type))
            {
                return null;
            }

            return GetSystemGroupFromPlayerLoopSystemNoChecks(ref playerLoopSystem);
        }
        
        /// <summary>
        /// Given a <see cref="PlayerLoopSystem"/>, gets the <see cref="ComponentSystemGroup"/> associated.
        /// This method has no checks and assumes that you are certain the <see cref="PlayerLoopSystem"/>
        /// does contain a <see cref="ComponentSystemGroup"/>. 
        /// </summary>
        /// <param name="playerLoopSystem">The <see cref="PlayerLoopSystem"/> to use.</param>
        /// <returns>
        /// The <see cref="ComponentSystemGroup"/> associated.
        /// </returns>
        public static ComponentSystemGroup GetSystemGroupFromPlayerLoopSystemNoChecks(ref PlayerLoopSystem playerLoopSystem)
        {
            return (ComponentSystemGroup)GetSystemFromPlayerLoopSystemNoChecks(ref playerLoopSystem);
        }

        /// <summary>
        /// Given a <see cref="PlayerLoopSystem"/>, gets the <see cref="ComponentSystemBase"/> associated.
        /// This is a "safe" method, it does not assume there is a <see cref="ComponentSystemBase"/> to get
        /// and may return null.
        /// </summary>
        /// <param name="playerLoopSystem">The <see cref="PlayerLoopSystem"/> to use.</param>
        /// <returns>
        /// The <see cref="ComponentSystemBase"/> if it exists.
        /// null if this <see cref="PlayerLoopSystem"/> is a phase
        /// null if this <see cref="PlayerLoopSystem"/> does not have a <see cref="ComponentSystemBase"/>
        /// </returns>
        public static ComponentSystemBase GetSystemFromPlayerLoopSystem(ref PlayerLoopSystem playerLoopSystem)
        {
            if (playerLoopSystem.updateDelegate?.Target == null)
            {
                return null;
            }

            return GetSystemFromPlayerLoopSystemNoChecks(ref playerLoopSystem);
        }

        /// <summary>
        /// Given a <see cref="PlayerLoopSystem"/>, gets the <see cref="ComponentSystemBase"/> associated.
        /// This method has no checks and assumes that you are certain the <see cref="PlayerLoopSystem"/>
        /// does contain a <see cref="ComponentSystemBase"/>. 
        /// </summary>
        /// <param name="playerLoopSystem">The <see cref="PlayerLoopSystem"/> to use.</param>
        /// <returns>
        /// The <see cref="ComponentSystemBase"/> associated.
        /// </returns>
        public static ComponentSystemBase GetSystemFromPlayerLoopSystemNoChecks(ref PlayerLoopSystem playerLoopSystem)
        {
            object wrapper = playerLoopSystem.updateDelegate.Target;
            
            //We have to lazy create this because we don't have access to the wrapper's type until we see it at runtime.
            if (s_DummyDelegateWrapper_System == null)
            {
                s_DummyDelegateWrapper_System = wrapper.GetType().GetProperty("System", BindingFlags.Instance | BindingFlags.NonPublic);
            }
            
            //TODO: #28 We can speed the reflected call up by using Expression Trees to create a strongly typed delegate
            //We should be able to call with the params we have access to and pass in "object" for the param we don't.
            //In the expression lambda, we do a convert to the internal type defined on the method info.
            return (ComponentSystemBase)s_DummyDelegateWrapper_System.GetMethod.Invoke(wrapper, null);
        }
    }
}
