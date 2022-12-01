using System;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal abstract class AbstractTaskDriverSystem : AbstractAnvilSystemBase
    {
        private RootWorkload m_RootWorkload;
        

        protected override void OnDestroy()
        {
            m_RootWorkload?.Dispose();
            base.OnDestroy();
        }

        public RootWorkload GetOrCreateRootWorkload(AbstractTaskDriver taskDriver)
        {
            m_RootWorkload = new RootWorkload(taskDriver.World, taskDriver.GetType(), this);
            return m_RootWorkload;
        }

        protected override void OnUpdate()
        {
            Dependency = m_RootWorkload.Update(Dependency);
        }
    }
}
