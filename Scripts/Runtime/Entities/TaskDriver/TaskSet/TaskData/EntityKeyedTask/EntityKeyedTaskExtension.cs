namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    /// <summary>
    /// Helper methods when working with <see cref="IEntityKeyedTask"/> in jobs.
    /// These make it more clear what is happening when operating on <see cref="IEntityKeyedTask"/> instances
    /// in a job.
    /// </summary>
    public static class EntityKeyedTaskExtension
    {
        /// <summary>
        /// Used when the updating of a <see cref="IEntityKeyedTask"/> is complete and it should be resolved
        /// into an instance of data in a different location.
        /// </summary>
        /// <param name="instance">The instance of data that was being updated"/></param>
        /// <param name="resolvedInstance">The instance of data to write to that target location</param>
        /// <param name="updater">A reference to the <see cref="DataStreamUpdater{TInstance}"/></param>
        /// <typeparam name="TInstance">The type of instance</typeparam>
        /// <typeparam name="TResolveTargetType">The type of the resolved data</typeparam>
        public static void Resolve<TInstance, TResolveTargetType>(
            this TInstance instance,
            TResolveTargetType resolvedInstance,
            ref DataStreamUpdater<TInstance> updater)
            where TInstance : unmanaged, IEntityKeyedTask
            where TResolveTargetType : unmanaged, IEntityKeyedTask
        {
            Resolve(instance, ref resolvedInstance, ref updater);
        }

        /// <summary>
        /// Used when the cancelling of a <see cref="IEntityKeyedTask"/> is complete and it should be resolved
        /// into an instance of data in a different location.
        /// </summary>
        /// <param name="instance">The instance of data that was being cancelled"/></param>
        /// <param name="resolvedInstance">The instance of data to write to that target location</param>
        /// <param name="updater">A reference to the <see cref="DataStreamCancellationUpdater{TInstance}"/></param>
        /// <typeparam name="TInstance">The type of instance</typeparam>
        /// <typeparam name="TResolveTargetType">The type of the resolved data</typeparam>
        public static void Resolve<TInstance, TResolveTargetType>(
            this TInstance instance,
            TResolveTargetType resolvedInstance,
            ref DataStreamCancellationUpdater<TInstance> updater)
            where TInstance : unmanaged, IEntityKeyedTask
            where TResolveTargetType : unmanaged, IEntityKeyedTask
        {
            Resolve(instance, ref resolvedInstance, ref updater);
        }

        /// <inheritdoc cref="Resolve{TInstance,TResolveTargetType}(TInstance,TResolveTargetType,ref Anvil.Unity.DOTS.Entities.TaskDriver.DataStreamUpdater{TInstance})"/>
        public static void Resolve<TInstance, TResolveTargetType>(
            this TInstance instance,
            ref TResolveTargetType resolvedInstance,
            ref DataStreamUpdater<TInstance> updater)
            where TInstance : unmanaged, IEntityKeyedTask
            where TResolveTargetType : unmanaged, IEntityKeyedTask
        {
            updater.Resolve(ref resolvedInstance);
        }

        /// <inheritdoc cref="Resolve{TInstance,TResolveTargetType}(TInstance,TResolveTargetType,ref Anvil.Unity.DOTS.Entities.TaskDriver.DataStreamCancellationUpdater{TInstance})"/>
        public static void Resolve<TInstance, TResolveTargetType>(
            this TInstance instance,
            ref TResolveTargetType resolvedInstance,
            ref DataStreamCancellationUpdater<TInstance> updater)
            where TInstance : unmanaged, IEntityKeyedTask
            where TResolveTargetType : unmanaged, IEntityKeyedTask
        {
            updater.Resolve(ref resolvedInstance);
        }

        /// <summary>
        /// Used when the updating of a <see cref="IEntityKeyedTask"/> is complete but nothing needs to be written
        /// to signify this completion. The instance will cease to exist.
        /// </summary>
        /// <param name="instance">The instance of data that was being updated</param>
        /// <param name="updater">A reference to the <see cref="DataStreamUpdater{TInstance}"/></param>
        /// <typeparam name="TInstance">The type of instance</typeparam>
        public static void Resolve<TInstance>(this TInstance instance, ref DataStreamUpdater<TInstance> updater)
            where TInstance : unmanaged, IEntityKeyedTask
        {
            updater.Resolve();
        }

        /// <summary>
        /// Used when the cancelling of a <see cref="IEntityKeyedTask"/> is complete but nothing needs to be written
        /// to signify this completion. The instance will cease to exist.
        /// </summary>
        /// <param name="instance">The instance of data that was being cancelled</param>
        /// <param name="updater">A reference to the <see cref="DataStreamCancellationUpdater{TInstance}"/></param>
        /// <typeparam name="TInstance">The type of instance</typeparam>
        public static void Resolve<TInstance>(this TInstance instance, ref DataStreamCancellationUpdater<TInstance> updater)
            where TInstance : unmanaged, IEntityKeyedTask
        {
            updater.Resolve();
        }

        /// <inheritdoc cref="DataStreamUpdater{TInstance}.Continue(ref TInstance)"/>
        public static void ContinueOn<TInstance>(this TInstance instance, ref DataStreamUpdater<TInstance> updater)
            where TInstance : unmanaged, IEntityKeyedTask
        {
            updater.Continue(ref instance);
        }

        /// <inheritdoc cref="DataStreamCancellationUpdater{TInstance}.Continue(ref TInstance)"/>
        public static void ContinueOn<TInstance>(this TInstance instance, ref DataStreamCancellationUpdater<TInstance> updater)
            where TInstance : unmanaged, IEntityKeyedTask
        {
            updater.Continue(ref instance);
        }
    }
}
