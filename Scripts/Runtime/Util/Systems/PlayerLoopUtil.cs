using System;
using System.Reflection;
using Unity.Entities;
using UnityEngine.LowLevel;

namespace Anvil.Unity.DOTS.Util
{
    public static class PlayerLoopUtil
    {
        private static readonly Type COMPONENT_SYSTEM_GROUP_TYPE = typeof(ComponentSystemGroup);
        
        private delegate bool IsDelegateForWorldSystemDelegate(World world, PlayerLoopSystem playerLoopSystem);
    
        private static readonly MethodInfo s_ScriptBehaviourUpdateOrder_IsDelegateForWorldSystem_MethodInfo = typeof(ScriptBehaviourUpdateOrder).GetMethod("IsDelegateForWorldSystem", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly IsDelegateForWorldSystemDelegate s_IsDelegateForWorldSystem = (IsDelegateForWorldSystemDelegate)Delegate.CreateDelegate(typeof(IsDelegateForWorldSystemDelegate), s_ScriptBehaviourUpdateOrder_IsDelegateForWorldSystem_MethodInfo);
        private static PropertyInfo s_DummyDelegateWrapper_System;
        
        
        public static bool IsPlayerLoopSystemPartOfWorld(ref PlayerLoopSystem playerLoopSystem, World world)
        {
            return playerLoopSystem.updateDelegate?.Target != null && s_IsDelegateForWorldSystem(world, playerLoopSystem);
        }

        public static ComponentSystemGroup GetSystemGroupFromPlayerLoopSystem(ref PlayerLoopSystem playerLoopSystem)
        {
            if (playerLoopSystem.updateDelegate?.Target == null
             || !COMPONENT_SYSTEM_GROUP_TYPE.IsAssignableFrom(playerLoopSystem.type))
            {
                return null;
            }

            return GetSystemGroupFromPlayerLoopSystemNoChecks(ref playerLoopSystem);
        }

        public static ComponentSystemGroup GetSystemGroupFromPlayerLoopSystemNoChecks(ref PlayerLoopSystem playerLoopSystem)
        {
            return (ComponentSystemGroup)GetSystemFromPlayerLoopSystemNoChecks(ref playerLoopSystem);
        }

        public static ComponentSystemBase GetSystemFromPlayerLoopSystem(ref PlayerLoopSystem playerLoopSystem)
        {
            if (playerLoopSystem.updateDelegate?.Target == null)
            {
                return null;
            }

            return GetSystemFromPlayerLoopSystemNoChecks(ref playerLoopSystem);
        }

        public static ComponentSystemBase GetSystemFromPlayerLoopSystemNoChecks(ref PlayerLoopSystem playerLoopSystem)
        {
            object wrapper = playerLoopSystem.updateDelegate.Target;
            
            if (s_DummyDelegateWrapper_System == null)
            {
                s_DummyDelegateWrapper_System = wrapper.GetType().GetProperty("System", BindingFlags.Instance | BindingFlags.NonPublic);
            }
            
            //TODO: We can speed the reflected call up by using Expression Trees to create a strongly typed delegate
            //We should be able to call with the params we have access to and pass in "object" for the param we don't.
            //In the expression lambda, we do a convert to the internal type defined on the method info.
            return (ComponentSystemBase)s_DummyDelegateWrapper_System.GetMethod.Invoke(wrapper, null);
        }
        
        
    }
}
