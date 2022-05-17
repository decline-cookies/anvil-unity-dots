using Anvil.Unity.DOTS.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// A system that exists to take advantage of the lifecycle of a world but does not actually need to update.
    /// Used for lookups to get/store data.
    /// </summary>
    public abstract partial class AbstractDataSystem : AbstractAnvilSystemBase
    {
        protected sealed override void OnCreate()
        {
            base.OnCreate();
            Init();
            Enabled = false;
        }

        protected abstract void Init();

        protected sealed override void OnUpdate()
        {
            //DOES NOTHING
        }
    }
}
