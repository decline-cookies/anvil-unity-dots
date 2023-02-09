using Anvil.CSharp.Core;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// A convenient implementation of <see cref="IComponentReferencable"/> that automatically registers and
    /// unregisters itself with <see cref="ManagedReferenceStore"/>.
    /// </summary>
    public abstract class AbstractComponentReferencable : AbstractAnvilBase, IComponentReferencable
    {
        protected AbstractComponentReferencable()
        {
            ManagedReferenceStore.RegisterReference(this);
        }

        protected override void DisposeSelf()
        {
            ManagedReferenceStore.UnregisterReference(this);

            base.DisposeSelf();
        }
    }
}
