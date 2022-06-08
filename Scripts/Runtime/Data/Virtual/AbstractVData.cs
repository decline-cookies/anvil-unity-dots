using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Jobs;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Collections;
using Unity.Jobs;
using Debug = UnityEngine.Debug;

namespace Anvil.Unity.DOTS.Data
{
    //TODO: Serialization and Deserialization
    public abstract class AbstractVData : AbstractAnvilBase
    {
        protected class NullVData : AbstractVData
        {
        }

        protected static readonly NullVData NULL_VDATA = new NullVData();

        private readonly AbstractVData m_Input;
        private readonly HashSet<AbstractVData> m_Outputs = new HashSet<AbstractVData>();


#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private const string STATE_UNACQUIRED = "Unacquired";
        private const string STATE_FOR_OUTPUT = "ForOutput";
        
        private string m_State;
        private string m_AcquireCallerInfo;
        private string m_ReleaseCallerInfo;
#endif

        protected AccessController AccessController
        {
            get;
        }

        internal AbstractVData()
        {
            Debug.Assert(GetType() == typeof(NullVData), $"Incorrect code path");
        }

        protected AbstractVData(AbstractVData input)
        {
            Debug.Assert(input != null, $"{this} was created by passing in a source but that source is null! Double check that it has been created.");
        
            AccessController = new AccessController();
            m_Input = input;
            m_State = STATE_UNACQUIRED;

            if (!(m_Input is NullVData))
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
            foreach (AbstractVData destinationData in m_Outputs)
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

            foreach (AbstractVData destinationData in m_Outputs)
            {
                destinationData.ReleaseForOutputAsync(releaseAccessDependency);
            }
        }

        private void RegisterOutput(AbstractVData output)
        {
            m_Outputs.Add(output);
        }

        private void UnregisterOutput(AbstractVData output)
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
            Debug.Assert(m_State == STATE_UNACQUIRED, $"{this} - State was {m_State} but expected {STATE_UNACQUIRED}. Corresponding release method was not called after: {m_AcquireCallerInfo}");
            m_State = newState;
            StackFrame frame = new StackFrame(4, true);
            m_AcquireCallerInfo = $"{frame.GetMethod().Name} at {frame.GetFileName()}:{frame.GetFileLineNumber()}";
        }
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        protected void ValidateReleaseState(string expectedState)
        {
            Debug.Assert(m_State == expectedState, $"{this} - State was {m_State} but expected {expectedState}. A release method was called an additional time after: {m_ReleaseCallerInfo}");
            m_State = STATE_UNACQUIRED;
            StackFrame frame = new StackFrame(4, true);
            m_ReleaseCallerInfo = $"{frame.GetMethod().Name} at {frame.GetFileName()}:{frame.GetFileLineNumber()}";
        }
    }
}
