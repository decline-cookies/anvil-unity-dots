using Anvil.CSharp.Core;
using Unity.Collections;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal class DestinationWorldDataMap : AbstractAnvilBase
    {
        public NativeParallelHashMap<uint, uint> TaskSetOwnerIDMapping;
        public NativeParallelHashMap<uint, uint> ActiveIDMapping;

        public DestinationWorldDataMap(
            NativeParallelHashMap<uint, uint> taskSetOwnerIDMapping, 
            NativeParallelHashMap<uint, uint> activeIDMapping)
        {
            TaskSetOwnerIDMapping = taskSetOwnerIDMapping;
            ActiveIDMapping = activeIDMapping;
        }

        protected override void DisposeSelf()
        {
            TaskSetOwnerIDMapping.Dispose();
            ActiveIDMapping.Dispose();
            base.DisposeSelf();
        }
    }
}
