using Anvil.CSharp.Core;
using System.Collections.Generic;
using Unity.Collections;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal class DestinationWorldDataMap : AbstractAnvilBase
    {
        public NativeParallelHashMap<TaskSetOwnerID, TaskSetOwnerID> TaskSetOwnerIDMapping;
        public NativeParallelHashMap<DataTargetID, DataTargetID> DataTargetIDMapping;

        public DestinationWorldDataMap(
            Dictionary<string, TaskSetOwnerID> srcTaskSetOwnerMapping,
            Dictionary<string, TaskSetOwnerID> dstTaskSetOwnerMapping,
            Dictionary<string, DataTargetID> srcDataTargetIDMapping,
            Dictionary<string, DataTargetID> dstDataTargetIDMapping)
        {
            //TODO: Optimization - Could instead pass in the list of entities that moved and use that as the upper limit. In most cases it will be less than the dstMapping counts unless its a full world move.
            //We're going to the Destination World so we can't have more than they have
            TaskSetOwnerIDMapping = new NativeParallelHashMap<TaskSetOwnerID, TaskSetOwnerID>(dstTaskSetOwnerMapping.Count, Allocator.Persistent);
            DataTargetIDMapping = new NativeParallelHashMap<DataTargetID, DataTargetID>(dstDataTargetIDMapping.Count, Allocator.Persistent);
            
            foreach (KeyValuePair<string, TaskSetOwnerID> entry in srcTaskSetOwnerMapping)
            {
                if (!dstTaskSetOwnerMapping.TryGetValue(entry.Key, out TaskSetOwnerID dstTaskSetOwnerID))
                {
                    continue;
                }
                TaskSetOwnerIDMapping.Add(entry.Value, dstTaskSetOwnerID);
            }
                
            foreach (KeyValuePair<string, DataTargetID> entry in srcDataTargetIDMapping)
            {
                if (!dstDataTargetIDMapping.TryGetValue(entry.Key, out DataTargetID dstDataTargetID))
                {
                    continue;
                }
                DataTargetIDMapping.Add(entry.Value, dstDataTargetID);
            }
        }

        protected override void DisposeSelf()
        {
            TaskSetOwnerIDMapping.Dispose();
            DataTargetIDMapping.Dispose();
            base.DisposeSelf();
        }
    }
}
