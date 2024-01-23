using Unity.Burst;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Core
{
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    public partial struct SharedWriteHandleSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            SharedWriteHandleManager.Reset();
        }
    }
}