using Anvil.Unity.DOTS.Jobs;
using Unity.Collections;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal unsafe class NativeArrayAccessWrapper<T> : AbstractAccessWrapper
        where T : struct
    {
        public NativeArray<T> NativeArray { get; }

        public NativeArrayAccessWrapper(NativeArray<T> nativeArray) : base(AccessType.SharedRead)
        {
            NativeArray = nativeArray;
        }


        public sealed override JobHandle Acquire()
        {
            //Does nothing - We don't know what the access could be here, up to the author to manage
            return default;
        }

        public sealed override void Release(JobHandle releaseAccessDependency)
        {
            //Does nothing - We don't know what the access could be here, up to the author to manage
        }
    }
}
