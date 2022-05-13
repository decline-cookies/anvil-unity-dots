using Anvil.CSharp.Logging;
using Anvil.Unity.DOTS.Logging;
using Unity.Collections;

public static class LoggerExtension
{
    public static BurstableLogger<FixedString32Bytes> AsBurstable(this in Log.Logger logger, string appendToMessagePrefix = null)
    {
        return AsBurstable<FixedString32Bytes>(logger, appendToMessagePrefix);
    }

    public static BurstableLogger<PrefixStringType> AsBurstable<PrefixStringType>(this in Log.Logger logger, string appendToMessagePrefix = null) where PrefixStringType : struct, INativeList<byte>, IUTF8Bytes
    {
        return new BurstableLogger<PrefixStringType>(logger, appendToMessagePrefix);
    }
}