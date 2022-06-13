using Anvil.Unity.DOTS.Data;
using System;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    public abstract class AbstractAsyncPopulator<TKey, TSource, TResult> : AbstractPopulator<TKey, TSource, TResult>
        where TKey : struct, IEquatable<TKey>
        where TSource : struct, ILookupValue<TKey>
        where TResult : struct, ILookupValue<TKey>
    {
        public abstract JobHandle PopulateAsync(JobHandle dependsOn, JobSourceWriter<TSource> sourceWriter, JobResultWriter<TResult> resultWriter);
        
        internal sealed override JobHandle Populate(JobHandle dependsOn, VirtualData<TKey, TSource> sourceData, VirtualData<TKey, TResult> resultData)
        {
            JobHandle addHandle = sourceData.AcquireForAddAsync(out JobSourceWriter<TSource> addStruct);
            JobResultWriter<TResult> resultStruct = resultData.GetCompletionWriter();
            
            JobHandle prePopulate = JobHandle.CombineDependencies(addHandle, dependsOn);
            
            JobHandle postPopulate = PopulateAsync(prePopulate, addStruct, resultStruct);
            
            sourceData.ReleaseForEntitiesAddAsync(postPopulate);
            
            //TODO: Could add a hook for user processing 
            
            return postPopulate;
        }
    }
}
