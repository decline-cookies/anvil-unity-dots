using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Anvil.CSharp.Logging;
using Anvil.Unity.DOTS.Jobs;
using System;
using Logger = Anvil.CSharp.Logging.Logger;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// The base class for Systems when using the Anvil Framework.
    /// Adds some convenience functionality non-release safety checks for
    /// <see cref="SystemBase"/> implementations.
    /// </summary>
    public abstract partial class AbstractAnvilSystemBase : SystemBase
    {
        private Logger? m_Logger;
        /// <summary>
        /// Returns a <see cref="Logger"/> for this instance to emit log messages with.
        /// Lazy instantiated.
        /// </summary>
        protected Logger Logger
        {
            get => m_Logger ?? (m_Logger = Log.GetLogger(this)).Value;
            set => m_Logger = value;
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        /// <inheritdoc cref="Dependency" />
        protected new JobHandle Dependency
        {
            get => base.Dependency;
            // Detects situations where the existing dependency is overwritten rather than chained or combined.
            set
            {
                if (!value.DependsOn(base.Dependency))
                {
                    throw new InvalidOperationException($"Dependency Chain Broken: Dependency Chain Broken: The incoming dependency does not contain the existing dependency in the chain.");
                }

                base.Dependency = value;
            }
        }
#endif

        /// <summary>
        /// Creates a new <see cref="AbstractAnvilSystemBase"/> instance.
        /// </summary>
        public AbstractAnvilSystemBase() : base()
        {
        }

        // ----- Copy From Buffers ----- //
        /// <summary>
        /// Schedule a job to asynchronously copy a singleton <see cref="DynamicBuffer{T}" /> to
        /// a <see cref="NativeArray{T}" /> after <see cref="Dependency"/> has completed.
        /// </summary>
        /// <typeparam name="T">The element type of the <see cref="DynamicBuffer{T}" />.</typeparam>
        /// <param name="outputBuffer">The <see cref="NativeArray{T}" /> to copy to.</param>
        /// <remarks>Actual copy is performed by <see cref="CopyFromSingletonBuffer{T}" /></remarks>
        protected void CopyFromSingletonBufferAsync<T>(NativeArray<T> outputBuffer) where T : struct, IBufferElementData
        {
            Dependency = CopyFromSingletonBufferAsync<T>(Dependency, outputBuffer);
        }

        /// <summary>
        /// Schedule a job to asynchronously copy a singleton <see cref="DynamicBuffer{T}" /> to
        /// a <see cref="NativeArray{T}" /> after the provided <see cref="JobHandle"/> has completed.
        /// </summary>
        /// <typeparam name="T">The element type of the <see cref="DynamicBuffer{T}" />.</typeparam>
        /// <param name="dependsOn">The <see cref="JobHandle"/> to wait for.</param>
        /// <param name="outputBuffer">The <see cref="NativeArray{T}" /> to copy to.</param>
        /// <returns>A <see cref="JobHandle"/> that represents when the buffer copy is complete.</returns>
        /// <remarks>Actual copy is performed by <see cref="CopyFromSingletonBuffer{T}" /></remarks>
        protected JobHandle CopyFromSingletonBufferAsync<T>(in JobHandle dependsOn, in NativeArray<T> outputBuffer) where T : struct, IBufferElementData
        {
            EntityQuery query = GetEntityQuery(ComponentType.ReadOnly<T>());
            CopyFromSingletonBuffer<T> job = new CopyFromSingletonBuffer<T>()
            {
                InputBufferTypeHandle = GetBufferTypeHandle<T>(),
                OutputBuffer = outputBuffer
            };
            return job.Schedule(query, dependsOn);
        }

        // ----- Copy To Buffers ----- //
        /// <summary>
        /// Schedule a job to asynchronously copy a <see cref="NativeArray{T}" /> to a
        /// singleton <see cref="DynamicBuffer{T}" /> after <see cref="Dependency"/> has completed.
        /// </summary>
        /// <typeparam name="T">The element type of the <see cref="DynamicBuffer{T}" />.</typeparam>
        /// <param name="inputBuffer">The <see cref="NativeArray{T}" /> to copy from.</param>
        /// <remarks>Actual copy is performed by <see cref="CopyToSingletonBuffer{T}" /></remarks>
        protected void CopyToSingletonBufferAsync<T>(in NativeArray<T> inputBuffer) where T : struct, IBufferElementData
        {
            Dependency = CopyToSingletonBufferAsync<T>(Dependency, inputBuffer);
        }

        /// /// <summary>
        /// Schedule a job to asynchronously copy a <see cref="NativeArray{T}" /> to a
        /// singleton <see cref="DynamicBuffer{T}" /> after the provided <see cref="JobHandle"/> has completed.
        /// </summary>
        /// <typeparam name="T">The element type of the <see cref="DynamicBuffer{T}" />.</typeparam>
        /// <param name="dependsOn">The <see cref="JobHandle"/> to wait for.</param>
        /// <param name="inputBuffer">The <see cref="NativeArray{T}" /> to copy from.</param>
        /// <returns>A <see cref="JobHandle"/> that represents when the buffer copy is complete.</returns>
        /// <remarks>Actual copy is performed by <see cref="CopyToSingletonBuffer{T}" /></remarks>
        protected JobHandle CopyToSingletonBufferAsync<T>(in JobHandle dependsOn, in NativeArray<T> inputBuffer) where T : struct, IBufferElementData
        {
            EntityQuery query = GetEntityQuery(ComponentType.ReadWrite<T>());
            CopyToSingletonBuffer<T> job = new CopyToSingletonBuffer<T>()
            {
                InputBuffer = inputBuffer,
                OutputBufferTypeHandle = GetBufferTypeHandle<T>()
            };
            return job.Schedule(query, dependsOn);
        }

    }
}