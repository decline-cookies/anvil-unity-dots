using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal static class EntityProxyInstanceWrapperExtension
    {
        public static unsafe void PatchIDs<TInstance>(
            this ref EntityProxyInstanceWrapper<TInstance> instanceWrapper, 
            uint taskSetOwnerID, 
            uint activeID)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            byte* ptr = (byte*)UnsafeUtility.AddressOf(ref instanceWrapper);
            uint* taskSetOwnerIDPtr = (uint*)(ptr + EntityProxyInstanceID.TASK_SET_OWNER_ID_OFFSET);
            *taskSetOwnerIDPtr = taskSetOwnerID;
            uint* activeIDPtr = (uint*)(ptr + EntityProxyInstanceID.ACTIVE_ID_OFFSET);
            *activeIDPtr = activeID;
        }
    }
}
