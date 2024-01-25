using Anvil.Unity.DOTS.Jobs;
using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    [BurstCompile]
    public static class SystemStateExtensions
    {
        [BurstCompile]
        public static void GetSystemDependency(this ref SystemState state, out JobHandle dependsOn)
        {
            dependsOn = state.Dependency;
        }

        [BurstCompile]
        public static void SetSystemDependency(this ref SystemState state, in JobHandle dependsOn)
        {
#if ANVIL_DEBUG_SAFETY
            if (!dependsOn.DependsOn(state.Dependency))
            {
                throw new InvalidOperationException($"Dependency Chain Broken: Dependency Chain Broken: The incoming dependency does not contain the existing dependency in the chain.");
            }
#endif
            state.Dependency = dependsOn;
        }
    }
}