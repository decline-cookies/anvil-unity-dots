using Unity.Collections;

namespace Anvil.Unity.DOTS.Core
{
    /// <summary>
    /// Interface for a burst compatible equivalent to <see cref="object.ToString"/>. When implemented, an object will create
    /// a fixed string (Ex: <see cref="FixedString512Bytes"/>) that represents the current object.
    /// </summary>
    /// <typeparam name="T">The fixed string type to return.</typeparam>
    public interface IToFixedString<out T> where T : struct, INativeList<byte>, IUTF8Bytes
    {
        /// <summary>
        /// Returns a burst compatible string of the current object.
        /// </summary>
        /// <returns>A fixed string that represents the current object.</returns>
        T ToFixedString();
    }
}