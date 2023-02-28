using System;
using Unity.Entities;
using UnityEngine;
using UnityEngine.LowLevel;

/// <summary>
/// A collection of methods that supplement <see cref="ScriptBehaviourUpdateOrder"/>'s functionality and require access
/// to internal types/methods.
/// </summary>
public static class ScriptBehaviourUpdateOrderInternal
{
    /// <summary>
    /// Given a <see cref="PlayerLoopSystem"/>, try to get the <see cref="ComponentSystemBase"/> associated.
    /// (non-recursive)
    /// NOTE: DO NOT use this class. Instead use <see cref="PlayerLoopUtil.TryGetSystemFromPlayerLoop"/>.
    /// </summary>
    /// <param name="playerLoopSystem">The <see cref="PlayerLoopSystem"/> to use.</param>
    /// <param name="system">The <see cref="ComponentSystemBase"/> associated.</param>
    /// <returns>
    /// True if there is a system.
    /// False if the provided player loop does not represent a system.
    /// </returns>
    /// <remarks>
    /// This is required because
    /// <see cref="ScriptBehaviourUpdateOrder.AppendSystemToPlayerLoop"/> wraps the update call in a dummy class to work
    /// around a limitation with Mono (<see cref="ScriptBehaviourUpdateOrder.DummyDelegateWrapper"/>).
    ///
    /// The method will be removed and most of the logic moved to <see cref="PlayerLoopUtil.TryGetSystemFromPlayerLoop"/>
    /// when Unity stops wrapping systems in <see cref="ScriptBehaviourUpdateOrder.DummyDelegateWrappper"/>.
    ///
    /// It should only be called by the <see cref="PlayerLoopUtil.TryGetSystemFromPlayerLoop"/> method.
    /// </remarks>
    public static bool TryGetSystemFromPlayerLoopSystem(ref PlayerLoopSystem playerLoopSystem, out ComponentSystemBase system)
    {
        if (playerLoopSystem.updateDelegate?.Target == null || !typeof(ComponentSystemBase).IsAssignableFrom(playerLoopSystem.type))
        {
            system = null;
            return false;
        }

        var wrapper = playerLoopSystem.updateDelegate.Target as ScriptBehaviourUpdateOrder.DummyDelegateWrapper;
        // If the wrapper is null then Unity has stopped wrapping component systems in DummyDelegateWrapper and we can
        // move this method out into the non assembly injected/internal version of this class.
        Debug.Assert(
            wrapper != null,
            $"Wrapper is null. Has Unity stopped wrapping top level systems in {nameof(ScriptBehaviourUpdateOrder.DummyDelegateWrapper)}?");

        system = wrapper.System;
        return true;
    }
}