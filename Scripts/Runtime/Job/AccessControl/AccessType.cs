using System;

namespace Anvil.Unity.DOTS.Jobs
{
    /// <summary>
    /// Represents the type of access to a collection for use with a <see cref="AccessController"/>
    /// </summary>
    public enum AccessType
    {
        ExclusiveWrite,
        SharedWrite,
        SharedRead,
        Disposal
    }

    /// <summary>
    /// A collection of extensions for <see cref="AccessType"/>.
    /// </summary>
    public static class AccessTypeExtension
    {
        /// <summary>
        /// Identifies whether the <see cref="AccessType"/> accommodates the operations of the supplied
        /// <see cref="targetType"/>.
        /// </summary>
        /// <param name="type">The type to check for support of the other type's operations.</param>
        /// <param name="targetType">The type to check for support of.</param>
        /// <returns>True if <see cref="type"/> supports the operations required by <see cref="targetType"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if the access type is not yet supported by this method.
        /// </exception>
        /// <example>
        /// An <see cref="AccessType"/> of <see cref="AccessType.SharedWrite"/> supports all of the operations needed
        /// for <see cref="AccessType.SharedRead"/> access.
        /// </example>
        public static bool IsCompatibleWith(this AccessType type, AccessType targetType)
        {
            return type == targetType
                || type switch
                {
                    AccessType.ExclusiveWrite => targetType is AccessType.SharedWrite or AccessType.SharedRead,
                    AccessType.SharedWrite => targetType is AccessType.SharedRead,
                    AccessType.SharedRead => false,
                    AccessType.Disposal => false,
                    _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
                };
        }
    }
}