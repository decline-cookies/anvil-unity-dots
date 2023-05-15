using Anvil.CSharp.Data;
using System;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine;

namespace Anvil.Unity.DOTS.Entities
{
    public struct EntityCommandBufferWithID
    {
        public readonly uint ID;
        [NativeDisableContainerSafetyRestriction][NativeDisableUnsafePtrRestriction] public EntityCommandBuffer EntityCommandBuffer;
        
        public EntityCommandBufferWithID(EntityCommandBuffer entityCommandBuffer) : this()
        {
            EntityCommandBuffer = entityCommandBuffer;
            ID = EntityCommandBufferWithIDManager.Register(this);
        }
        
        //*************************************************************************************************************
        // ENTITY COMMAND BUFFER API REPLICATION
        //*************************************************************************************************************

        public void SetComponent<T>(Entity e, T component) where T : struct, IComponentData
        {
            EntityCommandBuffer.SetComponent(e, component);
        }

        public DynamicBuffer<T> SetBuffer<T>(Entity e) where T : struct, IBufferElementData
        {
            return EntityCommandBuffer.SetBuffer<T>(e);
        }

        public void AppendToBuffer<T>(Entity e, T element) where T : struct, IBufferElementData
        {
            EntityCommandBuffer.AppendToBuffer(e, element);
        }

        public void AddComponent<T>(Entity e) where T : struct, IComponentData
        {
            EntityCommandBuffer.AddComponent<T>(e);
        }

        public void AddComponent<T>(Entity e, T component) where T : struct, IComponentData
        {
            EntityCommandBuffer.AddComponent(e, component);
        }

        public void AddComponent(Entity e, ComponentTypes componentTypes)
        {
            EntityCommandBuffer.AddComponent(e, componentTypes);
        }
    }
    
    //*************************************************************************************************************
    // STATIC REGISTRATION
    //*************************************************************************************************************

    public static class EntityCommandBufferWithIDManager
    {
        private static IDProvider s_CommandBufferIDProvider;
        private static Dictionary<uint, EntityCommandBufferWithID> s_CommandBufferLookup;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            s_CommandBufferIDProvider?.Dispose();
            s_CommandBufferIDProvider = new IDProvider();

            s_CommandBufferLookup?.Clear();
            s_CommandBufferLookup = new Dictionary<uint, EntityCommandBufferWithID>();
        }

        public static uint Register(EntityCommandBufferWithID ecb)
        {
            uint id = s_CommandBufferIDProvider.GetNextID();
            s_CommandBufferLookup.Add(id, ecb);
            return id;
        }

        public static EntityCommandBuffer GetECBByID(uint id)
        {
            if (!s_CommandBufferLookup.TryGetValue(id, out EntityCommandBufferWithID ecbWithID))
            {
                throw new InvalidOperationException($"Tried to get {nameof(EntityCommandBufferWithID)} by ID of {id} but it wasn't in the lookup!");
            }
            return ecbWithID.EntityCommandBuffer;
        }
    }
}
