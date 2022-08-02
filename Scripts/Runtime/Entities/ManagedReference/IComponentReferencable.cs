namespace Anvil.Unity.DOTS.Data
{
    /// <summary>
    /// Identifies that a managed instance is registered to <see cref="ManagedReferenceStore"/> and can be represented
    /// with a <see cref="ManagedReference{T}"/> using <see cref="IComponentReferencableExtension.AsComponentDataReference"/>.
    /// </summary>
    /// <remarks>
    /// Either subclass <see cref="AbstractComponentReferencable"/> or reference its implementation for correct usage.
    /// </remarks>
    public interface IComponentReferencable { }
}