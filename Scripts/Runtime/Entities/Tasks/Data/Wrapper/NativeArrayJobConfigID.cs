using System;
using Unity.Collections;

namespace Anvil.Unity.DOTS.Entities
{
    internal readonly struct NativeArrayJobConfigID
    {
        public static NativeArrayJobConfigID Create<T>(NativeArray<T> array, JobConfig.Usage usage)
            where T : unmanaged
        {
            return new NativeArrayJobConfigID(typeof(T), usage);
        }
        
        public Type Type
        {
            get;
        }

        public JobConfig.Usage Usage
        {
            get;
        }

        public NativeArrayJobConfigID(Type type, JobConfig.Usage usage)
        {
            Type = type;
            Usage = usage;
        }
    }
}
