using System.Runtime.CompilerServices;
using Unity.Mathematics;
using DateTime = System.DateTime;
using RuntimeInitializeLoadType = UnityEngine.RuntimeInitializeLoadType;

namespace Anvil.Unity.DOTS.Util
{
    /// <summary>
    /// Utility class for simplifying getting access to a <see cref="Random"/> instance.
    /// </summary>
    public static class RandomUtil
    {
        private static Random s_InternalRandom;

        //With DOTS, it's recommended to disable the Domain Reload when entering play mode as it will be pretty slow.
        //https://docs.unity3d.com/Packages/com.unity.entities@0.17/manual/install_setup.html
        //Because of this, statics will preserve state across play sessions. By using the RuntimeInitializeOnLoadMethod
        //we can ensure we reset the state on every play session.
        //RuntimeInitializeLoadType.SubsystemRegistration represents the earliest moment of all the entries in the enum.
        [UnityEngine.RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            s_InternalRandom = new Random((uint)DateTime.Now.Ticks);
        }
        
        /// <summary>
        /// Creates a new instance of a <see cref="Random"/> and gives it a random seed between 1 and uint.MaxValue
        /// </summary>
        /// <remarks>
        /// While you could create a new instance yourself, choosing the seed can be annoying. Common approaches are to
        /// use a different random number generator to generate the seed value or use some aspect of the current time.
        /// The issue with using the current time is that if you create a bunch of <see cref="Random"/> instance at the
        /// same time in a loop, the seeds will all be the same and the random numbers generated will all be the same.
        /// This method uses an internal instance of <see cref="Random"/> to generate a new seed and ensures that it
        /// fits the range requirements of the seed not being 0. You are guaranteed to have different seeds each time
        /// this is called.
        /// </remarks>
        /// <returns>A <see cref="Random"/> instance seeded with a random seed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Random CreateSeeded()
        {
            return new Random(s_InternalRandom.NextUInt(1, uint.MaxValue));
        }
    }
}
