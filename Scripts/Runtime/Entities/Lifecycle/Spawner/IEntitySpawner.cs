using Anvil.CSharp.Core;
using System;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    internal interface IEntitySpawner : IAnvilDisposable
    {
        internal event Action<IEntitySpawner> OnPendingWorkAdded;
        
        public uint EntityCommandBufferID { get; }

        internal JobHandle Schedule(
            JobHandle dependsOn,
            ref EntityCommandBufferWithID ecb);
    }
}