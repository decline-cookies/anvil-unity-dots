using System.Runtime.CompilerServices;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Anvil.Unity.DOTS.Util
{
    /// <summary>
    /// Extension methods for use with a <see cref="Random"/> instance.
    /// </summary>
    public static class RandomExtensions
    {
        private const uint SPREAD = uint.MaxValue / JobsUtility.MaxJobThreadCount;
        
        //TODO: Revisit when we get C# 8 support in Unity ECS https://github.com/decline-cookies/anvil-unity-dots/issues/3
        /// <summary>
        /// Given an instance of <see cref="Random"/>, this extension modifies the <see cref="Random"/>'s state so that
        /// multiple threads using the same initial state will output different random values based on the thread
        /// index.
        /// </summary>
        /// <remarks>
        /// A typical use case with <see cref="Random"/> is to create an instance on the main thread with a given seed.
        /// Then pass that instance into a job which will get copied across many different threads to do some work that
        /// requires randomized values.
        ///
        /// Because the instance is copied into each of the threads they all have the same state and will all output the
        /// exact same "random" values which defeats the purpose. This function uses the unique thread index and
        /// multiplies it by a spread to take up an evenly distributed wide range of bits of
        /// an <see cref="uint.MaxValue"/>. This value is then XOR'd with the existing state resulting in each thread
        /// having it's own unique state capable of producing widely divergent random numbers from each other.
        /// </remarks>
        /// <param name="random">A ref to the <see cref="Random"/> instance to modify</param>
        /// <param name="nativeThreadIndex">The thread index to modify this the state with</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UpdateSeedWithNativeThreadIndex(this ref Random random, int nativeThreadIndex)
        {
            random.InitState((uint)(random.state ^ (nativeThreadIndex * SPREAD)));
        }
    }
}
