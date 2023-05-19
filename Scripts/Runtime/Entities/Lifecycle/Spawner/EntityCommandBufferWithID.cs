using Anvil.CSharp.Data;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine;

namespace Anvil.Unity.DOTS.Entities
{
    public struct EntityCommandBufferWithID
    {
        public static uint GetPersistentID()
        {
            return EntityCommandBufferWithIDManager.GetNextID();
        }

        public static void ClearPreviousInstanceIfExists(uint id)
        {
            EntityCommandBufferWithIDManager.Unregister(id);
        }
        

        public readonly uint ID;

        [NativeDisableContainerSafetyRestriction] [NativeDisableUnsafePtrRestriction]
        private EntityCommandBuffer m_EntityCommandBuffer;

        /// <summary>
        /// Creates a new wrapper for an <see cref="EntityCommandBuffer"/> with an ID
        /// </summary>
        /// <param name="id">The ID to use to represent this <see cref="EntityCommandBuffer"/></param>
        /// <param name="entityCommandBufferSystem">
        /// An optional <see cref="EntityCommandBufferSystem"/>
        /// If null, the underlying <see cref="EntityCommandBuffer"/> will be created directly with <see cref="Allocator.Temp"/>
        /// for immediate use.
        /// If not null, then the underlying <see cref="EntityCommandBuffer"/> will be created off of the system.
        /// </param>
        public EntityCommandBufferWithID(uint id, EntityCommandBufferSystem entityCommandBufferSystem = null)
        {
            ID = id;
            m_EntityCommandBuffer = entityCommandBufferSystem?.CreateCommandBuffer() ?? new EntityCommandBuffer(Allocator.Temp);
            EntityCommandBufferWithIDManager.Register(this);
        }

        //*************************************************************************************************************
        // SPECIAL COMMAND BUFFER API REPLICATION
        //*************************************************************************************************************

        public static void SetSharedComponent<T>(uint id, Entity e, T component) where T : struct, ISharedComponentData
        {
            EntityCommandBuffer entityCommandBuffer = EntityCommandBufferWithIDManager.GetECBByID(id);
            entityCommandBuffer.SetSharedComponent(e, component);
        }

        //*************************************************************************************************************
        // ENTITY COMMAND BUFFER API REPLICATION
        //*************************************************************************************************************

        public void SetComponent<T>(Entity e, T component) where T : struct, IComponentData
        {
            m_EntityCommandBuffer.SetComponent(e, component);
        }

        public DynamicBuffer<T> SetBuffer<T>(Entity e) where T : struct, IBufferElementData
        {
            return m_EntityCommandBuffer.SetBuffer<T>(e);
        }

        public void AppendToBuffer<T>(Entity e, T element) where T : struct, IBufferElementData
        {
            m_EntityCommandBuffer.AppendToBuffer(e, element);
        }

        public void AddComponent<T>(Entity e) where T : struct, IComponentData
        {
            m_EntityCommandBuffer.AddComponent<T>(e);
        }

        public void AddComponent<T>(Entity e, T component) where T : struct, IComponentData
        {
            m_EntityCommandBuffer.AddComponent(e, component);
        }

        public void AddComponent(Entity e, ComponentTypes componentTypes)
        {
            m_EntityCommandBuffer.AddComponent(e, componentTypes);
        }

        public void DestroyEntity(Entity e)
        {
            m_EntityCommandBuffer.DestroyEntity(e);
        }

        public void Playback(EntityManager mgr)
        {
            m_EntityCommandBuffer.Playback(mgr);
        }

        public void Dispose()
        {
            m_EntityCommandBuffer.Dispose();
            ClearPreviousInstanceIfExists(ID);
        }

        public Entity CreateEntity(EntityArchetype archetype)
        {
            return m_EntityCommandBuffer.CreateEntity(archetype);
        }

        public Entity Instantiate(Entity e)
        {
            return m_EntityCommandBuffer.Instantiate(e);
        }

        //*************************************************************************************************************
        // STATIC REGISTRATION
        //*************************************************************************************************************

        internal static class EntityCommandBufferWithIDManager
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

            public static uint GetNextID()
            {
                return s_CommandBufferIDProvider.GetNextID();
            }

            public static void Unregister(uint id)
            {
                s_CommandBufferLookup.Remove(id);
            }

            public static void Register(EntityCommandBufferWithID ecb)
            {
                if (s_CommandBufferLookup.ContainsKey(ecb.ID))
                {
                    throw new InvalidOperationException($"Trying to make use of an {nameof(EntityCommandBufferWithID)} with ID {ecb.ID} but it is already in use!");
                }
                s_CommandBufferLookup.Add(ecb.ID, ecb);
            }

            public static EntityCommandBuffer GetECBByID(uint id)
            {
                if (!s_CommandBufferLookup.TryGetValue(id, out EntityCommandBufferWithID ecbWithID))
                {
                    throw new InvalidOperationException($"Tried to get {nameof(EntityCommandBufferWithID)} by ID of {id} but it wasn't in the lookup!");
                }
                return ecbWithID.m_EntityCommandBuffer;
            }
        }
    }
}
