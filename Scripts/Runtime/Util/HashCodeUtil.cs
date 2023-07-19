using System.Runtime.CompilerServices;
using Unity.Burst;

namespace Anvil.Unity.DOTS.Util
{
    /// <summary>
    /// Helper class for generating hash codes that is Burst compatible.
    /// </summary>
    [BurstCompile]
    public static class HashCodeUtil
    {
        /// <summary>
        /// Returns a hash code that combines two initial hash codes.
        /// </summary>
        /// <param name="h1">The first hash code</param>
        /// <param name="h2">The second hash code</param>
        /// <returns>A combined hash code</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetHashCode(int h1, int h2)
        {
            //Taken from ValueTuple.cs GetHashCode.
            //https://github.com/dotnet/roslyn/blob/main/src/Compilers/Test/Resources/Core/NetFX/ValueTuple/ValueTuple.cs
            //Licence is a-ok as per the top of the linked file:
            // - Licensed to the .NET Foundation under one or more agreements.
            // - The .NET Foundation licenses this file to you under the MIT license.
            // - See the LICENSE file in the project root for more information.
            //Unfortunately we can't use directly because it has a static Random class it creates which doesn't jive with Burst
            uint rol5 = ((uint)h1 << 5) | ((uint)h1 >> 27);
            return ((int)rol5 + h1) ^ h2;
        }

        /// <summary>
        /// Returns a hash code that combines three initial hash codes.
        /// </summary>
        /// <param name="h1">The first hash code</param>
        /// <param name="h2">The second hash code</param>
        /// <param name="h3">The third hash code</param>
        /// <returns>A combined hash code</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetHashCode(int h1, int h2, int h3)
        {
            return GetHashCode(
                GetHashCode(h1, h2),
                h3);
        }

        /// <summary>
        /// Returns a hash code that combines four initial hash codes.
        /// </summary>
        /// <param name="h1">The first hash code</param>
        /// <param name="h2">The second hash code</param>
        /// <param name="h3">The third hash code</param>
        /// <param name="h4">The fourth hash code</param>
        /// <returns>A combined hash code</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetHashCode(int h1, int h2, int h3, int h4)
        {
            return GetHashCode(
                GetHashCode(h1, h2),
                GetHashCode(h3, h4));
        }
    }
}