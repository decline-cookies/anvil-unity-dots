using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Jobs;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Collections;
using Unity.Jobs;
using Debug = UnityEngine.Debug;

namespace Anvil.Unity.DOTS.Data
{
    
    //TODO: Can probably merge AbstractVirtualData into VirtualData, might just need an interface
    //TODO: Serialization and Deserialization
    public abstract class AbstractVirtualData : AbstractAnvilBase
    {
        //TODO: Could there ever be more than one? 
        private readonly AbstractVirtualData m_Input;
        private readonly HashSet<AbstractVirtualData> m_Outputs = new HashSet<AbstractVirtualData>();
        
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private const string STATE_UNACQUIRED = "Unacquired";
        private const string STATE_FOR_OUTPUT = "ForOutput";
        
        private string m_State;
#endif

        internal AccessController AccessController
        {
            get;
        }

        protected AbstractVirtualData(AbstractVirtualData input)
        {
            AccessController = new AccessController();
            m_Input = input;
            m_State = STATE_UNACQUIRED;

            if (m_Input != null)
            {
                RegisterOutput(m_Input);
            }
        }

        protected override void DisposeSelf()
        {
            m_Input?.UnregisterOutput(this);
            AccessController.Dispose();
            m_Outputs.Clear();

            base.DisposeSelf();
        }

        public abstract JobHandle ConsolidateForFrame(JobHandle dependsOn);

        protected JobHandle AcquireOutputsAsync(JobHandle dependsOn)
        {
            if (m_Outputs.Count == 0)
            {
                return dependsOn;
            }

            //Get write access to all possible channels that we can write a response to.
            //+1 to include the incoming dependency
            NativeArray<JobHandle> allDependencies = new NativeArray<JobHandle>(m_Outputs.Count + 1, Allocator.Temp);
            allDependencies[0] = dependsOn;
            int index = 1;
            foreach (AbstractVirtualData destinationData in m_Outputs)
            {
                allDependencies[index] = destinationData.AcquireForOutputAsync();
                index++;
            }

            return JobHandle.CombineDependencies(allDependencies);
        }

        protected void ReleaseOutputsAsync(JobHandle releaseAccessDependency)
        {
            if (m_Outputs.Count == 0)
            {
                return;
            }

            foreach (AbstractVirtualData destinationData in m_Outputs)
            {
                destinationData.ReleaseForOutputAsync(releaseAccessDependency);
            }
        }

        private void RegisterOutput(AbstractVirtualData output)
        {
            m_Outputs.Add(output);
        }

        private void UnregisterOutput(AbstractVirtualData output)
        {
            m_Outputs.Remove(output);
        }

        private JobHandle AcquireForOutputAsync()
        {
            ValidateAcquireState(STATE_FOR_OUTPUT);
            return AccessController.AcquireAsync(AccessType.SharedWrite);
        }

        private void ReleaseForOutputAsync(JobHandle releaseAccessDependency)
        {
            ValidateReleaseState(STATE_FOR_OUTPUT);
            AccessController.ReleaseAsync(releaseAccessDependency);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        protected void ValidateAcquireState(string newState)
        {
            // Debug.Assert(m_State == STATE_UNACQUIRED, $"{this} - State was {m_State} but expected {STATE_UNACQUIRED}. Corresponding release method was not called after last acquire.");
            Debug.Assert(m_State == STATE_UNACQUIRED);
            m_State = newState;
        }
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        protected void ValidateReleaseState(string expectedState)
        {
            // Debug.Assert(m_State == expectedState, $"{this} - State was {m_State} but expected {expectedState}. A release method was called an additional time after last release.");
            Debug.Assert(m_State == expectedState);
            m_State = STATE_UNACQUIRED;
        }
    }
}
