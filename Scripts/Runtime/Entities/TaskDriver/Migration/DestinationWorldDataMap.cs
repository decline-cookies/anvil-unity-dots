using Anvil.CSharp.Core;
using System.Collections.Generic;
using Unity.Collections;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal class DestinationWorldDataMap : AbstractAnvilBase
    {
        public NativeParallelHashMap<uint, uint> TaskSetOwnerIDMapping;
        public NativeParallelHashMap<uint, uint> ActiveIDMapping;

        public DestinationWorldDataMap(
            Dictionary<string, uint> srcTaskSetOwnerMapping,
            Dictionary<string, uint> dstTaskSetOwnerMapping,
            Dictionary<string, uint> srcActiveIDMapping,
            Dictionary<string, uint> dstActiveIDMapping)
        {
            //TODO: Optimization - Could instead pass in the list of entities that moved and use that as the upper limit. In most cases it will be less than the dstMapping counts unless its a full world move.
            //We're going to the Destination World so we can't have more than they have
            TaskSetOwnerIDMapping = new NativeParallelHashMap<uint, uint>(dstTaskSetOwnerMapping.Count, Allocator.Persistent);
            ActiveIDMapping = new NativeParallelHashMap<uint, uint>(dstActiveIDMapping.Count, Allocator.Persistent);
            
            foreach (KeyValuePair<string, uint> entry in srcTaskSetOwnerMapping)
            {
                if (!dstTaskSetOwnerMapping.TryGetValue(entry.Key, out uint dstTaskSetOwnerID))
                {
                    continue;
                }
                TaskSetOwnerIDMapping.Add(entry.Value, dstTaskSetOwnerID);
            }
                
            foreach (KeyValuePair<string, uint> entry in srcActiveIDMapping)
            {
                if (!dstActiveIDMapping.TryGetValue(entry.Key, out uint dstActiveID))
                {
                    continue;
                }
                ActiveIDMapping.Add(entry.Value, dstActiveID);
            }
        }

        protected override void DisposeSelf()
        {
            TaskSetOwnerIDMapping.Dispose();
            ActiveIDMapping.Dispose();
            base.DisposeSelf();
        }
    }
}
