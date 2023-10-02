using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// A system that exists to take advantage of the lifecycle of a world but does not actually need to update.
    /// Used for lookups to get/store data.
    /// </summary>
    [UpdateInGroup(typeof(Group))]
    public abstract partial class AbstractDataSystem : AbstractAnvilSystemBase
    {
        //TODO: #172 - Temp to hold all AbstractDataSystem classes until we remove them from the PlayerLoop
        [UpdateInGroup(typeof(InitializationSystemGroup))]
        private partial class Group : ComponentSystemGroup { }

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