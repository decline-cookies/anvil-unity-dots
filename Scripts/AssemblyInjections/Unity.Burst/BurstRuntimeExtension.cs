using Unity.Burst;

public static class BurstRuntimeExtension
{
    public static int GetBurstHashCode32(this string text)
    {
        return BurstRuntime.HashStringWithFNV1A32(text);
    }

    public static long GetBurstHashCode64(this string text)
    {
        return BurstRuntime.HashStringWithFNV1A64(text);
    }
}
