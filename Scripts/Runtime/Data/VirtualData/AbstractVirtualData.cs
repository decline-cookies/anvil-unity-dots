using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Collections;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Data
{
    /// <summary>
    /// For dealing with <see cref="VirtualData{TKey,TInstance}"/> in a generic way without having
    /// to know the types.
    /// </summary>
    public abstract class AbstractVirtualData : AbstractAnvilBase
    {
        internal static readonly BulkScheduleDelegate<AbstractVirtualData> CONSOLIDATE_FOR_FRAME_SCHEDULE_DELEGATE = BulkSchedulingUtil.CreateSchedulingDelegate<AbstractVirtualData>(nameof(ConsolidateForFrame), BindingFlags.Instance | BindingFlags.NonPublic);
        
        private AbstractVirtualData m_Source;

        internal AccessController AccessController { get; }
        internal Type Type { get; }

        private byte ResultDestinationType
        {
            get;
        }
        
        protected Dictionary<byte, AbstractVirtualData> ResultDestinations
        {
            get;
        }

        protected AbstractVirtualData(byte resultDestinationType)
        {
            ResultDestinationType = resultDestinationType;
            ResultDestinations = new Dictionary<byte, AbstractVirtualData>();
            AccessController = new AccessController();
            Type = GetType();
        }

        protected override void DisposeSelf()
        {
            RemoveFromSource();
            ResultDestinations.Clear();

            AccessController.Dispose();

            base.DisposeSelf();
        }
        
        internal abstract unsafe void* GetWriterPointer();
        
        //*************************************************************************************************************
        // RELATIONSHIPS
        //*************************************************************************************************************

        internal void AddResultDestination(byte resultDestinationType, AbstractVirtualData resultData)
        {
            ResultDestinations.Add(resultDestinationType, resultData);
        }

        private void RemoveResultDestination(byte resultDestinationType)
        {
            ResultDestinations.Remove(resultDestinationType);
        }

        internal void SetSource(AbstractVirtualData sourceData)
        {
            m_Source = sourceData;
        }

        private void RemoveFromSource()
        {
            m_Source?.RemoveResultDestination(ResultDestinationType);
        }

        //*************************************************************************************************************
        // ACCESS
        //*************************************************************************************************************
        internal JobHandle AcquireForUpdateAsync()
        {
            JobHandle exclusiveWrite = AccessController.AcquireAsync(AccessType.ExclusiveWrite);

            int len = ResultDestinations.Count;

            if (len == 0)
            {
                return exclusiveWrite;
            }

            //Get write access to all possible channels that we can write a result to.
            //+1 to include the exclusive write
            NativeArray<JobHandle> allDependencies = new NativeArray<JobHandle>(len + 1, Allocator.Temp);
            int index = 0;
            foreach (AbstractVirtualData destinationData in ResultDestinations.Values)
            {
                allDependencies[index] = destinationData.AccessController.AcquireAsync(AccessType.SharedWrite);
                index++;
            }

            allDependencies[len] = exclusiveWrite;

            return JobHandle.CombineDependencies(allDependencies);
        }

        internal void ReleaseForUpdateAsync(JobHandle releaseAccessDependency)
        {
            AccessController.ReleaseAsync(releaseAccessDependency);

            if (ResultDestinations.Count == 0)
            {
                return;
            }

            //Release all the possible channels we could have written a result to.
            foreach (AbstractVirtualData destinationData in ResultDestinations.Values)
            {
                destinationData.AccessController.ReleaseAsync(releaseAccessDependency);
            }
        }

        internal void AcquireForUpdate()
        {
            AccessController.Acquire(AccessType.ExclusiveWrite);

            foreach (AbstractVirtualData destinationData in ResultDestinations.Values)
            {
                destinationData.AccessController.Acquire(AccessType.SharedWrite);
            }
        }

        internal void ReleaseForUpdate()
        {
            AccessController.Release();

            foreach (AbstractVirtualData destinationData in ResultDestinations.Values)
            {
                destinationData.AccessController.Release();
            }
        }

        //*************************************************************************************************************
        // CONSOLIDATION
        //*************************************************************************************************************
        internal abstract JobHandle ConsolidateForFrame(JobHandle dependsOn);
    }
}
