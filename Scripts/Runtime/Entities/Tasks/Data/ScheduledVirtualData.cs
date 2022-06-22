using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using System;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    internal class ScheduledVirtualData
    {
        private delegate JobHandle AcquireDelegate();

        private delegate void ReleaseDelegate(JobHandle releaseAccessDependency);
        
        public JobDataContext Context
        {
            get;
        }

        public IVirtualData Data
        {
            get;
        }

        private readonly AcquireDelegate m_AcquireDelegate;
        private readonly ReleaseDelegate m_ReleaseDelegate;
        private readonly AccessController m_AccessController;
        
        public ScheduledVirtualData(IVirtualData data, JobDataContext context)
        {
            Data = data;
            m_AccessController = Data.AccessController;
            Context = context;
            m_AcquireDelegate = context switch
            {
                JobDataContext.Read               => AcquireForReadAsync,
                JobDataContext.Add                => AcquireForSharedWriteAsync,
                JobDataContext.Update             => AcquireForUpdate,
                JobDataContext.ResultsDestination => AcquireForResultsDestination,
                _                                 => throw new ArgumentOutOfRangeException(nameof(context), context, null)
            };

            m_ReleaseDelegate = context switch
            {
                JobDataContext.Read               => ReleaseAsync,
                JobDataContext.Add                => ReleaseAsync,
                JobDataContext.Update             => ReleaseAsync,
                JobDataContext.ResultsDestination => ReleaseResultsDestination,
                _                                 => throw new ArgumentOutOfRangeException(nameof(context), context, null)
            };
        }
        
        private JobHandle AcquireForReadAsync()
        {
            return m_AccessController.AcquireAsync(AccessType.SharedRead);
        }

        private JobHandle AcquireForSharedWriteAsync()
        {
            return m_AccessController.AcquireAsync(AccessType.SharedWrite);
        }

        private JobHandle AcquireForUpdate()
        {
            return Data.AcquireForUpdate();
        }

        private JobHandle AcquireForResultsDestination()
        {
            return default;
        }

        private void ReleaseAsync(JobHandle releaseAccessDependency)
        {
            Data.ReleaseForUpdate(releaseAccessDependency);
        }

        private void ReleaseResultsDestination(JobHandle releaseAccessDependency)
        {
            //Does Nothing
        }

        public JobHandle Acquire()
        {
            return m_AcquireDelegate();
        }

        public void Release(JobHandle releaseAccessDependency)
        {
            m_ReleaseDelegate(releaseAccessDependency);
        }
    }
}
