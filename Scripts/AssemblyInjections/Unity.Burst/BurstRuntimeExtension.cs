using System;
using Unity.Burst;

/// <summary>
/// Helper methods to get the same Hash that <see cref="BurstRuntime"/> generates for <see cref="Type"/> but for
/// an arbitrary string.
/// NOTE: These methods will NOT work in a Burst Context.
/// </summary>
public static class BurstRuntimeExtension
{
    /// <summary>
    /// Gets the 32 bit hash code for an arbitrary string.
    /// </summary>
    /// <param name="text">The text to generate the hash for</param>
    /// <returns>The hash code</returns>
    public static int GetBurstHashCode32(this string text)
    {
        return BurstRuntime.HashStringWithFNV1A32(text);
    }

    /// <summary>
    /// Gets the 64 bit hash code for an arbitrary string.
    /// </summary>
    /// <param name="text">The text to generate the hash for</param>
    /// <returns>The hash code</returns>
    public static long GetBurstHashCode64(this string text)
    {
        return BurstRuntime.HashStringWithFNV1A64(text);
    }
}
