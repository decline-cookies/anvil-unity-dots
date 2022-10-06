using System;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    /// <summary>
    /// Helper methods when working with <see cref="IEntityProxyInstance"/> in jobs.
    /// These make it more clear what is happening when operating on <see cref="IEntityProxyInstance"/> instances
    /// in a job.
    /// </summary>
    public static class EntityProxyInstanceExtension
    {
        /// <summary>
        /// Used when the updating of a <see cref="IEntityProxyInstance"/> is complete and it should be resolved
        /// into an instance of data in a different location.
        /// </summary>
        /// <param name="instance">The instance of data that was being updated"/></param>
        /// <param name="resolveTarget">The identifier for the target location</param>
        /// <param name="resolvedInstance">The instance of data to write to that target location</param>
        /// <param name="updater">A reference to the <see cref="DataStreamUpdater{TInstance}"/></param>
        /// <typeparam name="TInstance">The type of instance</typeparam>
        /// <typeparam name="TResolveTarget">The type of identifier</typeparam>
        /// <typeparam name="TResolvedInstance">The type of the resolved data</typeparam>
        //TODO: With #64, Mike was wondering if we can get rid of the Enum and instead use the "Type" instead
        public static void Resolve<TInstance, TResolveTarget, TResolvedInstance>(this TInstance instance,
                                                                                 TResolveTarget resolveTarget,
                                                                                 TResolvedInstance resolvedInstance,
                                                                                 ref DataStreamUpdater<TInstance> updater)
            where TInstance : unmanaged, IEntityProxyInstance
            where TResolveTarget : Enum
            where TResolvedInstance : unmanaged, IEntityProxyInstance
        {
            Resolve(instance, resolveTarget, ref resolvedInstance, ref updater);
        }
        
        /// <summary>
        /// Used when the cancelling of a <see cref="IEntityProxyInstance"/> is complete and it should be resolved
        /// into an instance of data in a different location.
        /// </summary>
        /// <param name="instance">The instance of data that was being cancelled"/></param>
        /// <param name="resolveTarget">The identifier for the target location</param>
        /// <param name="resolvedInstance">The instance of data to write to that target location</param>
        /// <param name="updater">A reference to the <see cref="DataStreamCancellationUpdater{TInstance}"/></param>
        /// <typeparam name="TInstance">The type of instance</typeparam>
        /// <typeparam name="TResolveTarget">The type of identifier</typeparam>
        /// <typeparam name="TResolvedInstance">The type of the resolved data</typeparam>
        public static void Resolve<TInstance, TResolveTarget, TResolvedInstance>(this TInstance instance,
                                                                                 TResolveTarget resolveTarget,
                                                                                 TResolvedInstance resolvedInstance,
                                                                                 ref DataStreamCancellationUpdater<TInstance> updater)
            where TInstance : unmanaged, IEntityProxyInstance
            where TResolveTarget : Enum
            where TResolvedInstance : unmanaged, IEntityProxyInstance
        {
            Resolve(instance, resolveTarget, ref resolvedInstance, ref updater);
        }
        
        /// <inheritdoc cref="Resolve{TInstance,TResolveTarget,TResolvedInstance}(TInstance,TResolveTarget,TResolvedInstance,ref Anvil.Unity.DOTS.Entities.Tasks.DataStreamUpdater{TInstance})"/>
        public static void Resolve<TInstance, TResolveTarget, TResolvedInstance>(this TInstance instance,
                                                                                 TResolveTarget resolveTarget,
                                                                                 ref TResolvedInstance resolvedInstance,
                                                                                 ref DataStreamUpdater<TInstance> updater)
            where TInstance : unmanaged, IEntityProxyInstance
            where TResolveTarget : Enum
            where TResolvedInstance : unmanaged, IEntityProxyInstance
        {
            updater.Resolve(resolveTarget, ref resolvedInstance);
        }
        
        /// <inheritdoc cref="Resolve{TInstance,TResolveTarget,TResolvedInstance}(TInstance,TResolveTarget,TResolvedInstance,ref Anvil.Unity.DOTS.Entities.Tasks.DataStreamCancellationUpdater{TInstance})"/>
        public static void Resolve<TInstance, TResolveTarget, TResolvedInstance>(this TInstance instance,
                                                                                 TResolveTarget resolveTarget,
                                                                                 ref TResolvedInstance resolvedInstance,
                                                                                 ref DataStreamCancellationUpdater<TInstance> updater)
            where TInstance : unmanaged, IEntityProxyInstance
            where TResolveTarget : Enum
            where TResolvedInstance : unmanaged, IEntityProxyInstance
        {
            updater.Resolve(resolveTarget, ref resolvedInstance);
        }

        /// <summary>
        /// Used when the updating of a <see cref="IEntityProxyInstance"/> is complete but nothing needs to be written
        /// to signify this completion. The instance will cease to exist.
        /// </summary>
        /// <param name="instance">The instance of data that was being updated</param>
        /// <param name="updater">A reference to the <see cref="DataStreamUpdater{TInstance}"/></param>
        /// <typeparam name="TInstance">The type of instance</typeparam>
        public static void Resolve<TInstance>(this TInstance instance, ref DataStreamUpdater<TInstance> updater)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            updater.Resolve();
        }
        
        /// <summary>
        /// Used when the cancelling of a <see cref="IEntityProxyInstance"/> is complete but nothing needs to be written
        /// to signify this completion. The instance will cease to exist.
        /// </summary>
        /// <param name="instance">The instance of data that was being cancelled</param>
        /// <param name="updater">A reference to the <see cref="DataStreamCancellationUpdater{TInstance}"/></param>
        /// <typeparam name="TInstance">The type of instance</typeparam>
        public static void Resolve<TInstance>(this TInstance instance, ref DataStreamCancellationUpdater<TInstance> updater)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            updater.Resolve();
        }

        /// <inheritdoc cref="DataStreamUpdater{TInstance}.Continue(ref TInstance)"/>
        public static void ContinueOn<TInstance>(this TInstance instance, ref DataStreamUpdater<TInstance> updater)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            updater.Continue(ref instance);
        }
        
        /// <inheritdoc cref="DataStreamCancellationUpdater{TInstance}.Continue(ref TInstance)"/>
        public static void ContinueOn<TInstance>(this TInstance instance, ref DataStreamCancellationUpdater<TInstance> updater)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            updater.Continue(ref instance);
        }
    }
}