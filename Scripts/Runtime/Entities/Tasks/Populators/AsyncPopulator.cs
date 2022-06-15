using Anvil.Unity.DOTS.Data;
using System;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    internal class AsyncPopulator<TTaskDriverSystem, TKey, TSource, TResult> : AbstractPopulator<TKey, TSource, TResult>
        where TTaskDriverSystem : AbstractTaskDriverSystem<TKey, TSource>
        where TKey : struct, IEquatable<TKey>
        where TSource : struct, ILookupData<TKey>
        where TResult : struct, ILookupData<TKey>
    {

        private readonly AbstractTaskDriver<TTaskDriverSystem, TKey, TSource, TResult>.PopulateAsyncDelegate m_PopulateDelegate;

        public AsyncPopulator(AbstractTaskDriver<TTaskDriverSystem, TKey, TSource, TResult>.PopulateAsyncDelegate populateDelegate)
        {
            m_PopulateDelegate = populateDelegate;
        }

        internal sealed override JobHandle Populate(JobHandle dependsOn, VirtualData<TKey, TSource> sourceData, VirtualData<TKey, TResult> resultData)
        {
            JobHandle addHandle = sourceData.AcquireForAddAsync(out JobInstanceWriter<TSource> addStruct);
            JobResultWriter<TResult> resultStruct = resultData.GetResultWriter();
            
            JobHandle prePopulate = JobHandle.CombineDependencies(addHandle, dependsOn);
            
            JobHandle postPopulate = m_PopulateDelegate(prePopulate, addStruct, resultStruct);
            
            sourceData.ReleaseForAddAsync(postPopulate);
            
            //TODO: Could add a hook for user processing 
            
            return postPopulate;
        }
    }
}
