using Anvil.Unity.DOTS.Data;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// A collection of extension methods for working with <see cref="ComponentSystemBaseExtension"/>
    /// </summary>
    public static class ComponentSystemBaseExtension
    {
        /// <summary>
        /// Builds a container to provide in job access to a singleton <see cref="DynamicBuffer{T}"/>.
        /// </summary>
        /// <param name="isReadOnly">true if the data will not be written to.</param>
        /// <typeparam name="T">The element type of the <see cref="DynamicBuffer{T}"/>.</typeparam>
        /// <returns>A container that provides in job access to the requested <see cref="DynamicBuffer{T}"/>.</returns>
        public static BufferFromSingleEntity<T> GetBufferFromSingletonEntity<T>(this ComponentSystemBase system, bool isReadOnly = false) where T : struct, IBufferElementData
        {
            return system.GetBufferFromEntity<T>(isReadOnly).ForSingleEntity(system.GetSingletonEntity<T>());
        }

        /// <summary>
        /// Builds a container to provide in job access to a <see cref="DynamicBuffer{T}"/> on an entity.
        /// </summary>
        /// <param name="entity">The <see cref="Entity"/> to get the <see cref="DynamicBuffer{T}"/> from.</param>
        /// <param name="isReadOnly">true if the data will not be written to.</param>
        /// <typeparam name="T">The element type of the <see cref="DynamicBuffer{T}"/>.</typeparam>
        /// <returns>A container that provides in job access to the requested <see cref="DynamicBuffer{T}"/>.</returns>
        /// <remarks>
        /// If fetching many single entity containers at once, getting a <see cref="BufferFromEntity{T}"/> instance and
        /// calling <see cref="BufferFromEntityExtension.ForSingleEntity{T}"/> is more efficient.
        /// </remarks>
        public static BufferFromSingleEntity<T> GetBufferFromSingleEntity<T>(this ComponentSystemBase system, Entity entity, bool isReadOnly = false) where T : struct, IBufferElementData
        {
            return system.GetBufferFromEntity<T>(isReadOnly).ForSingleEntity(entity);
        }

        /// <summary>
        /// Builds a container to provide in job access to a singleton <see cref="IComponentData"/>.
        /// </summary>
        /// <param name="isReadOnly">true if the data will not be written to.</param>
        /// <typeparam name="T">The type that implements <see cref="IComponentData"/>.</typeparam>
        /// <returns>A container that provides in job access to the requested <see cref="T"/>.</returns>
        public static ComponentDataFromSingleEntity<T> GetComponentDataFromSingletonEntity<T>(this ComponentSystemBase system, bool isReadOnly) where T : struct, IComponentData
        {
            return system.GetComponentDataFromEntity<T>(isReadOnly).ForSingleEntity(system.GetSingletonEntity<T>());
        }

        /// <summary>
        /// Builds a container to provide in job access to a <see cref="IComponentData{T}"/> on an entity.
        /// </summary>
        /// <param name="entity">The <see cref="Entity"/> to get the <see cref="T"/> from.</param>
        /// <param name="isReadOnly">true if the data will not be written to.</param>
        /// <typeparam name="T">The element type of the <see cref="IComponentData"/>.</typeparam>
        /// <returns>A container that provides in job access to the requested <see cref="T"/>.</returns>
        /// <remarks>
        /// If fetching many single entity containers at once, getting a <see cref="ComponentDataFromEntity{T}"/> instance and
        /// calling <see cref="ComponentDataFromEntityExtension.ForSingleEntity{T}"/> is more efficient.
        /// </remarks>
        public static ComponentDataFromSingleEntity<T> GetComponentDataFromSingleEntity<T>(this ComponentSystemBase system, Entity entity, bool isReadOnly) where T : struct, IComponentData
        {
            return system.GetComponentDataFromEntity<T>(isReadOnly).ForSingleEntity(entity);
        }

        /// <summary>
        /// Schedule a job to asynchronously copy a <see cref="DynamicBuffer{T}" /> to
        /// a <see cref="NativeArray{T}" /> after the provided <see cref="JobHandle"/> has completed.
        /// </summary>
        /// <typeparam name="T">The element type of the <see cref="DynamicBuffer{T}" />.</typeparam>
        /// <param name="dependsOn">The <see cref="JobHandle"/> to wait for.</param>
        /// <param name="fromEntity">The entity with the buffer to to read from.</param>
        /// <param name="outputBuffer">The <see cref="NativeArray{T}" /> to copy to.</param>
        /// <returns>A <see cref="JobHandle"/> that represents when the buffer copy is complete.</returns>
        /// <remarks>
        /// Actual copy is performed by <see cref="CopyBufferToNativeArray{T}" />. This is just a convenience method.
        /// </remarks>
        public static JobHandle CopyBufferToNativeArray<T>(this ComponentSystemBase system, in JobHandle dependsOn, Entity fromEntity, in NativeArray<T> outputBuffer) where T : struct, IBufferElementData
        {
            CopyBufferToNativeArray<T> job = new CopyBufferToNativeArray<T>()
            {
                InputBufferFromEntity = system.GetBufferFromEntity<T>(true).ForSingleEntity(fromEntity),
                OutputBuffer = outputBuffer
            };

            return job.Schedule(dependsOn);
        }

        /// <summary>
        /// Schedule a job to asynchronously copy a <see cref="DynamicBuffer{T}" /> to
        /// a <see cref="DeferredNativeArray{T}" /> after the provided <see cref="JobHandle"/> has completed.
        /// </summary>
        /// <typeparam name="T">The element type of the <see cref="DynamicBuffer{T}" />.</typeparam>
        /// <param name="dependsOn">The <see cref="JobHandle"/> to wait for.</param>
        /// <param name="fromEntity">The entity with the buffer to to read from.</param>
        /// <param name="outputBuffer">The <see cref="DeferredNativeArray{T}" /> to copy to.</param>
        /// <returns>A <see cref="JobHandle"/> that represents when the buffer copy is complete.</returns>
        /// <remarks>
        /// Actual copy is performed by <see cref="CopyBufferToDeferredNativeArray{T}" />. This is just a convenience
        /// method.
        /// </remarks>
        public static JobHandle CopyBufferToDeferredNativeArray<T>(this ComponentSystemBase system, in JobHandle dependsOn, Entity fromEntity, in DeferredNativeArray<T> outputBuffer) where T : unmanaged, IBufferElementData
        {
            CopyBufferToDeferredNativeArray<T> job = new CopyBufferToDeferredNativeArray<T>()
            {
                InputBufferFromEntity = system.GetBufferFromEntity<T>(true).ForSingleEntity(fromEntity),
                OutputBuffer = outputBuffer
            };

            return job.Schedule(dependsOn);
        }

        // ----- Copy To Buffer ----- //
        /// /// <summary>
        /// Schedule a job to asynchronously copy a <see cref="NativeArray{T}" /> to a <see cref="DynamicBuffer{T}" />
        /// after the provided <see cref="JobHandle"/> has completed.
        /// </summary>
        /// <typeparam name="T">The element type of the <see cref="DynamicBuffer{T}" />.</typeparam>
        /// <param name="dependsOn">The <see cref="JobHandle"/> to wait for.</param>
        /// <param name="inputBuffer">The <see cref="NativeArray{T}" /> to copy from.</param>
        /// <param name="toEntity">The entity with the buffer to write to.</param>
        /// <returns>A <see cref="JobHandle"/> that represents when the buffer copy is complete.</returns>
        /// <remarks>
        /// Actual copy is performed by <see cref="CopyNativeArrayToBuffer{T}" />. This is just a convenience method.
        /// </remarks>
        public static JobHandle CopyNativeArrayToBuffer<T>(this ComponentSystemBase system, in JobHandle dependsOn, in NativeArray<T> inputBuffer, Entity toEntity) where T : struct, IBufferElementData
        {
            CopyNativeArrayToBuffer<T> job = new CopyNativeArrayToBuffer<T>()
            {
                InputBuffer = inputBuffer,
                OutputBufferFromEntity = system.GetBufferFromEntity<T>(false).ForSingleEntity(toEntity)
            };

            return job.Schedule(dependsOn);
        }

        /// <summary>
        /// Attempts to find the parent <see cref="ComponentSystemGroup"/> of a <see cref="ComponentSystem"/>
        /// </summary>
        /// <param name="system">The system to find the parent of.</param>
        /// <param name="group">The parent of the system</param>
        /// <returns>True if a parent was found for the given system.</returns>
        public static bool TryFindParentGroup(this ComponentSystem system, out ComponentSystemGroup group)
        {
            var worldSystems = system.World.Systems;
            foreach (ComponentSystemBase worldSystem in worldSystems)
            {
                if (worldSystem is ComponentSystemGroup worldGroup)
                {
                    Debug.Assert(worldGroup.Systems is List<ComponentSystemBase>);
                    if ((worldGroup.Systems as List<ComponentSystemBase>).Contains(system))
                    {
                        group = worldGroup;
                        return true;
                    }
                }
            }

            group = null;
            return false;
        }
    }
}