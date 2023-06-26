using System;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal class ResolveTargetDefinition
    {
        public static unsafe ResolveTargetDefinition Create<TInstance>(void* pendingWriterPointer)
            where TInstance : unmanaged, IEntityKeyedTask
        {
            uint typeID = ResolveTargetUtil.RegisterResolveTarget<TInstance>();
            return new ResolveTargetDefinition(typeof(TInstance), typeID, (long)pendingWriterPointer);
        }

        public readonly Type Type;
        public readonly uint TypeID;
        public readonly long PendingWriterPointerAddress;

        private ResolveTargetDefinition(Type type, uint typeID, long pendingWriterPointerAddress)
        {
            Type = type;
            TypeID = typeID;
            PendingWriterPointerAddress = pendingWriterPointerAddress;
        }
    }
}
