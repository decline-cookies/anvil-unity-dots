namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal abstract class AbstractTaskDriverSystem : AbstractAnvilSystemBase
    {
        // ReSharper disable once UnassignedGetOnlyAutoProperty
        public CommonTaskSet CommonTaskSet { get; }
        
        protected override void OnDestroy()
        {
            CommonTaskSet.Dispose();
            base.OnDestroy();
        }
        
        protected override void OnUpdate()
        {
            Dependency = CommonTaskSet.Update(Dependency);
        }
    }
}
