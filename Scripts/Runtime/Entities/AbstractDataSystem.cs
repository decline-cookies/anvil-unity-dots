namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// A system that exists to take advantage of the lifecycle of a world but does not actually need to update.
    /// Used for lookups to get/store data.
    /// </summary>
    public abstract partial class AbstractDataSystem : AbstractAnvilSystemBase
    {
        //TODO: #172 - Remove from PlayerLoop
        protected override void OnCreate()
        {
            base.OnCreate();
            Enabled = false;
        }

        protected sealed override void OnUpdate()
        {
            //DOES NOTHING
        }
    }
}
