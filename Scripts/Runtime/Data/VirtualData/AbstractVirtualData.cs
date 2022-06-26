using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Collections.Generic;
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
        private readonly List<AbstractVirtualData> m_Sources;
        private readonly List<AbstractVirtualData> m_ResultDestinations;

        internal AccessController AccessController
        {
            get;
        }

        public Type Type
        {
            get;
        }

        protected AbstractVirtualData()
        {
            m_Sources = new List<AbstractVirtualData>();
            m_ResultDestinations = new List<AbstractVirtualData>();
            AccessController = new AccessController();
            Type = GetType();
        }

        protected override void DisposeSelf()
        {
            RemoveFromSources();
            m_ResultDestinations.Clear();
            m_Sources.Clear();

            AccessController.Dispose();
            
            base.DisposeSelf();
        }
        
        //*************************************************************************************************************
        // RELATIONSHIPS
        //*************************************************************************************************************

        internal void AddResultDestination(AbstractVirtualData resultData)
        {
            m_ResultDestinations.Add(resultData);
        }

        private void RemoveResultDestination(AbstractVirtualData resultData)
        {
            m_ResultDestinations.Remove(resultData);
        }

        internal void AddSource(AbstractVirtualData sourceData)
        {
            m_Sources.Add(sourceData);
        }

        private void RemoveSource(AbstractVirtualData sourceData)
        {
            m_Sources.Remove(sourceData);
        }

        private void RemoveFromSources()
        {
            foreach (AbstractVirtualData sourceData in m_Sources)
            {
                sourceData.RemoveResultDestination(this);
            }
        }
        
        //*************************************************************************************************************
        // ACCESS
        //*************************************************************************************************************
        internal JobHandle AcquireForUpdateAsync()
        {
            JobHandle exclusiveWrite = AccessController.AcquireAsync(AccessType.ExclusiveWrite);

            if (m_ResultDestinations.Count == 0)
            {
                return exclusiveWrite;
            }

            //Get write access to all possible channels that we can write a response to.
            //+1 to include the exclusive write
            int len = m_ResultDestinations.Count + 1;
            NativeArray<JobHandle> allDependencies = new NativeArray<JobHandle>(len, Allocator.Temp);
            len--;
            for (int i = 0; i < len; ++i)
            {
                AbstractVirtualData destinationData = m_ResultDestinations[i];
                allDependencies[i] = destinationData.AccessController.AcquireAsync(AccessType.SharedWrite);
            }

            allDependencies[len] = exclusiveWrite;

            return JobHandle.CombineDependencies(allDependencies);
        }

        internal void ReleaseForUpdateAsync(JobHandle releaseAccessDependency)
        {
            AccessController.ReleaseAsync(releaseAccessDependency);

            if (m_ResultDestinations.Count == 0)
            {
                return;
            }

            foreach (AbstractVirtualData destinationData in m_ResultDestinations)
            {
                destinationData.AccessController.ReleaseAsync(releaseAccessDependency);
            }
        }

        internal void AcquireForUpdate()
        {
            AccessController.Acquire(AccessType.ExclusiveWrite);

            foreach (AbstractVirtualData destinationData in m_ResultDestinations)
            {
                destinationData.AccessController.Acquire(AccessType.SharedWrite);
            }
        }

        internal void ReleaseForUpdate()
        {
            AccessController.Release();

            foreach (AbstractVirtualData destinationData in m_ResultDestinations)
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
