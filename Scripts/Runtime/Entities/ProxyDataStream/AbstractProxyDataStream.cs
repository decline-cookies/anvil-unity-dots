using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Reflection;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// For dealing with <see cref="ProxyDataStream{TData}"/> in a generic way without having
    /// to know the types.
    /// </summary>
    public abstract class AbstractProxyDataStream : AbstractAnvilBase
    {
        internal static readonly BulkScheduleDelegate<AbstractProxyDataStream> CONSOLIDATE_FOR_FRAME_SCHEDULE_DELEGATE = BulkSchedulingUtil.CreateSchedulingDelegate<AbstractProxyDataStream>(nameof(ConsolidateForFrame), BindingFlags.Instance | BindingFlags.NonPublic);
        
        //TODO: Lock down to internal again
        public AccessController AccessController { get; }
        internal Type Type { get; }

        //TODO: Rename to something better. VirtualData is ambiguous between one instance of data or the collection. This is more of a stream. Think on it.
        //TODO: Split VirtualData into two pieces of functionality.
        //TODO: 1. Data collection independent of the TaskDrivers all about Wide/Narrow and load balancing. 
        //TODO: 2. A mechanism to handle the branching from Data to a Result type
        //TODO: https://github.com/decline-cookies/anvil-unity-dots/pull/52/files#r960787785
        protected AbstractProxyDataStream()
        {
            //TODO: Could split the data into definitions via Attributes or some other mechanism to set up the relationships. Then a "baking" into the actual structures. 
            //TODO: https://github.com/decline-cookies/anvil-unity-dots/pull/52/files#r960764532
            //TODO: https://github.com/decline-cookies/anvil-unity-dots/pull/52/files#r960737069
            AccessController = new AccessController();
            Type = GetType();
        }

        protected override void DisposeSelf()
        {
            AccessController.Dispose();

            base.DisposeSelf();
        }
        
        internal abstract unsafe void* GetWriterPointer();
        internal abstract JobHandle ConsolidateForFrame(JobHandle dependsOn);
    }
}
