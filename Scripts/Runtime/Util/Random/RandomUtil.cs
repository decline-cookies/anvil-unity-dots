using System.Runtime.CompilerServices;
using Unity.Mathematics;
using DateTime = System.DateTime;
using RuntimeInitializeLoadType = UnityEngine.RuntimeInitializeLoadType;

namespace Anvil.Unity.DOTS.Systems
{
    /// <summary>
    /// Utility class for simplifying getting access to a <see cref="Random"/> instance.
    /// </summary>
    public static class RandomUtil
    {
        private static Random s_InternalRandom;

        // With the Burst compiler, it's common to disable the Domain Reload step when entering play mode to increase iteration time.
        // https://docs.unity3d.com/Packages/com.unity.entities@0.17/manual/install_setup.html
        // Because of this, statics will preserve state across play sessions.
        // `RuntimeInitializeOnLoadMethod` ensures we reset the state on every play session.
        // `RuntimeInitializeLoadType.SubsystemRegistration` represents the earliest moment of all the entries in the enum.
        [UnityEngine.RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            s_InternalRandom = new Random((uint)DateTime.Now.Ticks);
        }
        
        /// <summary>
        /// Creates a new instance of a <see cref="Random"/> and gives it a random seed between 1 and
        /// <see cref="uint.MaxValue"/>
        /// </summary>
        /// <remarks>
        /// While you could create a new instance yourself, choosing the seed can be annoying. Common approaches are to
        /// use a different random number generator to generate the seed value or use some aspect of the current time.
        /// Using current time when generating multiple random instances quickly (or in parallel) risks multiple
        /// instances sharing the same seed value and will therefore generate the same random numbers.
        /// This method uses an internal instance of <see cref="Random"/> to generate a new seed and ensures that it
        /// fits the range requirements of the seed not being 0.
        /// </remarks>
        /// <returns>A <see cref="Random"/> instance seeded with a random seed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Random CreateSeeded()
        {
            return new Random(s_InternalRandom.NextUInt(1, uint.MaxValue));
        }
    }
}
