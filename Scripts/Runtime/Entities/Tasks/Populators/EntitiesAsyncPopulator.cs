using Anvil.Unity.DOTS.Data;
using System;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    internal class EntitiesAsyncPopulator<TTaskDriverSystem, TKey, TSource, TResult> : AbstractPopulator<TKey, TSource, TResult>
        where TTaskDriverSystem : AbstractTaskDriverSystem<TKey, TSource>
        where TKey : struct, IEquatable<TKey>
        where TSource : struct, ILookupData<TKey>
        where TResult : struct, ILookupData<TKey>
    {
        private readonly AbstractTaskDriver<TTaskDriverSystem, TKey, TSource, TResult>.PopulateEntitiesAsyncDelegate m_PopulateDelegate;

        public EntitiesAsyncPopulator(AbstractTaskDriver<TTaskDriverSystem, TKey, TSource, TResult>.PopulateEntitiesAsyncDelegate populateDelegate)
        {
            m_PopulateDelegate = populateDelegate;
        }

        //TODO: Generalize this with AsyncPopulator
        internal sealed override JobHandle Populate(JobHandle dependsOn, VirtualData<TKey, TSource> sourceData, VirtualData<TKey, TResult> resultData)
        {
            JobHandle addHandle = sourceData.AcquireForEntitiesAddAsync(out JobInstanceWriterEntities<TSource> addStruct);
            JobResultWriter<TResult> resultStruct = resultData.GetResultWriter();
            
            JobHandle prePopulate = JobHandle.CombineDependencies(addHandle, dependsOn);
            
            JobHandle postPopulate = m_PopulateDelegate(prePopulate, addStruct, resultStruct);
            
            sourceData.ReleaseForEntitiesAddAsync(postPopulate);
            
            //TODO: Could add a hook for user processing 
            
            return postPopulate;
        }
    }
}
