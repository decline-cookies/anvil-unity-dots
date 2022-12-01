using System;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal abstract class AbstractTaskDriverSystem : AbstractAnvilSystemBase
    {
        public RootWorkload RootWorkload { get; }

        protected AbstractTaskDriverSystem()
        {
            Type systemType = GetType();
            Type taskDriverType = systemType.GenericTypeArguments[0];
            RootWorkload = new RootWorkload(taskDriverType, this);
        }

        protected override void OnDestroy()
        {
            RootWorkload.Dispose();
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            Dependency = RootWorkload.Update(Dependency);
        }
    }
}
