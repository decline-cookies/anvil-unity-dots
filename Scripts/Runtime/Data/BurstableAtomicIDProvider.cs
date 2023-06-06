using Anvil.CSharp.Data;
using System.Threading;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Anvil.Unity.DOTS.Data
{
    //TODO: Once we have CSharp's ability for generic math: https://learn.microsoft.com/en-ca/dotnet/csharp/whats-new/csharp-11#generic-math-support,
    //      we could have this implement a IIDProvider<T> instead.
    /// <summary>
    /// An <see cref="IDProvider"/> that is compatible with Burst and will get the next ID atomically so it is
    /// safe to use in threaded context.
    /// </summary>
    [BurstCompatible]
    public readonly unsafe struct BurstableAtomicIDProvider
    {
        public const uint DEFAULT_SUPPLY_WARNING_THRESHOLD = uint.MaxValue - 1_000_000;
        
        /// <summary>
        /// The threshold to signify the ID supply is near exhaustion
        /// </summary>
        public readonly uint SupplyWarningThreshold;
        
        [NativeDisableUnsafePtrRestriction] private readonly uint* m_IDPointer;
        
        //TODO: #265 - Build System to check if we've gone over the SupplyWarningThreshold
        /// <summary>
        /// Creates a new instance of <see cref="BurstableAtomicIDProvider"/>.
        /// </summary>
        /// <remarks>
        /// NOTE: Because this is a struct, it could be created by using the default constructor which will fail.
        /// Debug.Asserts will catch this at runtime when used.
        /// It is expected to create this struct and pass in an explicit supply warning threshold instead, the
        /// <see cref="DEFAULT_SUPPLY_WARNING_THRESHOLD"/> is provided to make this easy to do so.
        ///
        /// ALSO NOTE: Because this must be compatible with Burst, there is no event to signify when the
        /// <see cref="SupplyWarningThreshold"/> has been met like in <see cref="IDProvider"/>. Instead, you must
        /// poll this struct periodically and call <see cref="HasIDExceededSupplyWarningThreshold"/>.
        /// </remarks>
        /// <param name="supplyWarningThreshold">The threshold to signify the ID supply is near exhaustion</param>
        public BurstableAtomicIDProvider(uint supplyWarningThreshold)
        {
            m_IDPointer = (uint*)UnsafeUtility.Malloc(
                UnsafeUtility.SizeOf<uint>(),
                UnsafeUtility.AlignOf<uint>(),
                Allocator.Persistent);
            UnsafeUtility.MemClear(m_IDPointer, UnsafeUtility.SizeOf<uint>());

            SupplyWarningThreshold = supplyWarningThreshold;
        }

        /// <summary>
        /// Provides the next ID atomically
        /// </summary>
        /// <returns>The next ID to use</returns>
        public uint GetNextID()
        {
            //This will catch if anyone accidentally used the default constructor
            Debug.Assert(SupplyWarningThreshold > 0);

            return (uint)Interlocked.Add(ref UnsafeUtility.AsRef<int>(m_IDPointer), 1);
        }

        /// <summary>
        /// Returns whether the IDs have passed the warning threshold for supply running out.
        /// </summary>
        /// <returns>true if IDs have passed the threshold, false if not</returns>
        public bool HasIDExceededSupplyWarningThreshold()
        {
            //This will catch if anyone accidentally used the default constructor
            Debug.Assert(SupplyWarningThreshold > 0);

            long address = (long)m_IDPointer;
            uint currentID = (uint)Interlocked.Read(ref address);
            return currentID > SupplyWarningThreshold;
        }
    }
}
