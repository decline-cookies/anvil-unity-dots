using Anvil.CSharp.Core;

namespace Anvil.Unity.DOTS.Data
{
    public abstract class AbstractComponentReferencable : AbstractAnvilBase, IComponentReferencable
    {
        public AbstractComponentReferencable()
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