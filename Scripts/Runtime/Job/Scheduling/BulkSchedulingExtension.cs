using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Collections;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Jobs
{
    /// <summary>
    /// Extension methods for bulk scheduling jobs from a collection and chaining the dependencies properly.
    /// </summary>
    public static class BulkSchedulingExtension
    {
        //NOTE: While it's annoying, we're duplicating code for the different collections in order to avoid
        //generating garbage. Using ICollection<TElement> and the like results in boxing and/or the creation and
        //disposal of the Enumerator. By duplicating the code for each collection type we avoid this.
        //This is necessary because these functions often are run in hot sections of the code multiple times every frame.

        /// <summary>
        /// Calls the <see cref="BulkScheduleDelegate{T}"/> for every item in the collection to schedule a job.
        /// These jobs will all be scheduled to start at the same time based on the <paramref name="dependsOn"/> and
        /// will return a combined <see cref="JobHandle"/> when all are complete.
        /// </summary>
        /// <param name="array">The collection</param>
        /// <param name="dependsOn">The dependency to wait on before any of the jobs can start</param>
        /// <param name="dependenciesArrayScratchPad">A <see cref="NativeArray{T}"/> to hold job dependencies</param>
        /// <param name="scheduleFunc">The <see cref="BulkScheduleDelegate{T}"/> to call on each element</param>
        /// <typeparam name="TElement">The type of element in the collection</typeparam>
        /// <returns>A <see cref="JobHandle"/> that represents when all jobs are completed.</returns>
        public static JobHandle BulkScheduleParallel<TElement>(
            this TElement[] array,
            JobHandle dependsOn,
            ref NativeArray<JobHandle> dependenciesArrayScratchPad,
            BulkScheduleDelegate<TElement> scheduleFunc)
        {
            int len = array.Length;
            if (len == 0)
            {
                return dependsOn;
            }

            Debug_EnsureSameLengths(len, dependenciesArrayScratchPad.Length);

            for (int i = 0; i < len; ++i)
            {
                dependenciesArrayScratchPad[i] = scheduleFunc(array[i], dependsOn);
            }

            return JobHandle.CombineDependencies(dependenciesArrayScratchPad);
        }

        /// <inheritdoc cref="BulkScheduleParallel{TElement}"/>
        public static JobHandle BulkScheduleParallel<TKey, TElement>(
            this Dictionary<TKey, TElement>.ValueCollection valueCollection,
            JobHandle dependsOn,
            BulkScheduleDelegate<TElement> scheduleFunc)
        {
            int len = valueCollection.Count;
            if (len == 0)
            {
                return dependsOn;
            }

            NativeArray<JobHandle> dependencies = new NativeArray<JobHandle>(len, Allocator.Temp);
            int index = 0;
            foreach (TElement element in valueCollection)
            {
                dependencies[index] = scheduleFunc(element, dependsOn);
                index++;
            }

            return JobHandle.CombineDependencies(dependencies);
        }

        /// <summary>
        /// Calls the <see cref="BulkScheduleDelegate{T}"/> for every item in the collection to schedule a job.
        /// These jobs will be scheduled sequentially with the first job starting after <paramref name="dependsOn"/>
        /// is complete and each subsequent job after the one before it is complete.
        /// </summary>
        /// <param name="array">The collection</param>
        /// <param name="dependsOn">The dependency to wait on before the first job can start</param>
        /// <param name="scheduleFunc">The <see cref="BulkScheduleDelegate{T}"/> to call on each element</param>
        /// <typeparam name="TElement">The type of element in the collection</typeparam>
        /// <returns>A <see cref="JobHandle"/> that represents when the last job is completed.</returns>
        public static JobHandle BulkScheduleSequential<TElement>(
            this TElement[] array,
            JobHandle dependsOn,
            BulkScheduleDelegate<TElement> scheduleFunc)
        {
            int len = array.Length;
            if (len == 0)
            {
                return dependsOn;
            }

            for (int i = 0; i < len; ++i)
            {
                dependsOn = scheduleFunc(array[i], dependsOn);
            }

            return dependsOn;
        }

        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void Debug_EnsureSameLengths(int arrayLength, int scratchPadLength)
        {
            if (arrayLength != scratchPadLength)
            {
                throw new InvalidOperationException($"Trying to bulk schedule but the array length {arrayLength} doesn't match the dependencies native array length {scratchPadLength}!");
            }
        }
    }
}