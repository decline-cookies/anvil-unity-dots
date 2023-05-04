using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal static class EntityProxyInstanceWrapperExtension
    {
        // public static unsafe void PatchIDs<TInstance>(
        //     this ref EntityProxyInstanceWrapper<TInstance> instanceWrapper, 
        //     uint taskSetOwnerID, 
        //     uint activeID)
        //     where TInstance : unmanaged, IEntityProxyInstance
        // {
        //     //Get the address of where the EntityProxyInstanceID is in the wrapper
        //     byte* ptr = (byte*)UnsafeUtility.AddressOf(ref instanceWrapper) + EntityProxyInstanceWrapper<TInstance>.INSTANCE_ID_OFFSET;
        //     //Get the address for the TaskSetOwnerID and set it
        //     uint* taskSetOwnerIDPtr = (uint*)(ptr + EntityProxyInstanceID.TASK_SET_OWNER_ID_OFFSET);
        //     *taskSetOwnerIDPtr = taskSetOwnerID;
        //     //Get the address for the ActiveID and set it
        //     uint* activeIDPtr = (uint*)(ptr + EntityProxyInstanceID.DATA_TARGET_ID_OFFSET);
        //     *activeIDPtr = activeID;
        // }
        //
        // public static unsafe void PatchIDs(
        //     this ref EntityProxyInstanceID instanceID, 
        //     uint taskSetOwnerID, 
        //     uint activeID)
        // {
        //     //Get the address of the instance ID
        //     byte* ptr = (byte*)UnsafeUtility.AddressOf(ref instanceID);
        //     //Get the address for the TaskSetOwnerID and set it
        //     uint* taskSetOwnerIDPtr = (uint*)(ptr + EntityProxyInstanceID.TASK_SET_OWNER_ID_OFFSET);
        //     *taskSetOwnerIDPtr = taskSetOwnerID;
        //     //Get the address for the ActiveID and set it
        //     uint* activeIDPtr = (uint*)(ptr + EntityProxyInstanceID.DATA_TARGET_ID_OFFSET);
        //     *activeIDPtr = activeID;
        // }
    }
}
