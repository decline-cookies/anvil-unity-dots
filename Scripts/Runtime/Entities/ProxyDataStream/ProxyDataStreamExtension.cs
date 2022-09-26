using System;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// Helper methods when working with <see cref="ProxyDataStream{TInstance}"/>
    /// These make it more clear what is happening when operating on <see cref="ProxyDataStream{TInstance}"/> instances
    /// in a job.
    /// </summary>
    public static class ProxyDataStreamExtension
    {
        public static void Resolve<TInstance, TResolveTarget, TResolvedInstance>(this TInstance instance,
                                                                                 TResolveTarget resolveTarget,
                                                                                 TResolvedInstance resolvedInstance,
                                                                                 ref DataStreamUpdater<TInstance> updater)
            where TInstance : unmanaged, IProxyInstance
            where TResolveTarget : Enum
            where TResolvedInstance : unmanaged, IProxyInstance
        {
            Resolve(instance, resolveTarget, ref resolvedInstance, ref updater);
        }

        public static void Resolve<TInstance, TResolveTarget, TResolvedInstance>(this TInstance instance,
                                                                                 TResolveTarget resolveTarget,
                                                                                 ref TResolvedInstance resolvedInstance,
                                                                                 ref DataStreamUpdater<TInstance> updater)
            where TInstance : unmanaged, IProxyInstance
            where TResolveTarget : Enum
            where TResolvedInstance : unmanaged, IProxyInstance
        {
            updater.Resolve(resolveTarget, ref resolvedInstance);
        }

        //TODO: Code-stink with extension method that doesn't actually use the data - https://github.com/decline-cookies/anvil-unity-dots/pull/57#discussion_r967355560
        //TODO: DOCS
        public static void Resolve<TInstance>(this TInstance instance, ref DataStreamUpdater<TInstance> updater)
            where TInstance : unmanaged, IProxyInstance
        {
            updater.Resolve();
        }

        //TODO: DOCS
        public static void ContinueOn<TInstance>(this TInstance instance, ref DataStreamUpdater<TInstance> updater)
            where TInstance : unmanaged, IProxyInstance
        {
            updater.Continue(ref instance);
        }
    }
}
