using System.Collections.Generic;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    public partial class PropagateCancelSystem : AbstractAnvilSystemBase
    {
        private readonly List<AbstractTaskSystem> m_TaskSystemsToPropagateCancelFor;
        
        public PropagateCancelSystem()
        {
            m_TaskSystemsToPropagateCancelFor = new List<AbstractTaskSystem>();
        }

        public void RegisterTaskSystem(AbstractTaskSystem taskSystem)
        {
            m_TaskSystemsToPropagateCancelFor.Add(taskSystem);
            float a = 0.0f;
        }

        protected override void OnUpdate()
        {
            JobHandle dependsOn = Dependency;

            foreach (AbstractTaskSystem taskSystem in m_TaskSystemsToPropagateCancelFor)
            {
                dependsOn = taskSystem.PropagateCancel(dependsOn);
            }

            Dependency = dependsOn;
        }
    }
}
