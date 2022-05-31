using Anvil.Unity.DOTS.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Jobs
{
    /// <summary>
    /// Helper job to turn a <see cref="UnsafeTypedStream{T}"/> into a <see cref="NativeArray{T}"/>
    /// via a <see cref="DeferredNativeArray{T}"/>.
    /// Useful for load balancing from multiple threads writing variable amounts to each lane in the
    /// <see cref="UnsafeTypedStream{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type contained in the collection</typeparam>
    [BurstCompile]
    public struct ConsolidateToNativeArrayJob<T> : IJob
        where T : struct
    {
        [ReadOnly] private readonly UnsafeTypedStream<T>.Reader m_Reader;
        private DeferredNativeArray<T> m_DeferredNativeArray;

        public ConsolidateToNativeArrayJob(UnsafeTypedStream<T>.Reader reader,
                                           DeferredNativeArray<T> deferredNativeArray)
        {
            m_Reader = reader;
            m_DeferredNativeArray = deferredNativeArray;
        }

        public void Execute()
        {
            int newLength = m_Reader.Count();

            if (newLength == 0)
            {
                return;
            }

            NativeArray<T> array = m_DeferredNativeArray.DeferredCreate(newLength);
            m_Reader.CopyInto(ref array);
        }
    }
}
