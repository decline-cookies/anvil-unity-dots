using Unity.Collections;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Jobs
{
    /// <summary>
    /// A wrapper for NativeArray that provides a way to manage async access to the array.
    /// </summary>
    public class NativeArrayAccessController<T> : AbstractValueAccessController<NativeArray<T>> where T : struct
    {
        public NativeArrayAccessController(int length, NativeArrayOptions options)
            : base(new NativeArray<T>(length, Allocator.Persistent, options)) { }
    }
}