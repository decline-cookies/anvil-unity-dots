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
}
