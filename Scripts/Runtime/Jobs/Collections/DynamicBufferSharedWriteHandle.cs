using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Util;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Anvil.Unity.DOTS.Jobs
{
    /// <summary>
    /// A utility class that handles managing access for shared writing (multiple job types writing at the same time)
    /// so that jobs can be scheduled easily.
    ///
    /// ****IMPORTANT NOTE****
    /// Using this class is very helpful for the specific task of writing to a <see cref="DynamicBuffer{T}"/> in
    /// parallel from multiple job types. This class DOES NOT prevent multiple threads from writing to the same element.
    /// Typically, this means that the use case should guarantee that there is no overlap in buffer element access.
    /// For example, element ranges (1-n) are dedicated to specific worker indices.
    /// (Worker 0 writes to element 0, Worker 3 writes to element 3, etc...)
    ///
    /// In order to achieve this functionality the class tracks the World's system order to the best of its ability.
    /// There are cases where this isn't perfect and your won't get the results you expect.
    /// These cases are outlined below to aid in debugging if needed.
    ///
    /// ALL OF THESE SHOULD BE VERY RARE
    ///
    /// - If you are adding/removing/reordering Systems and SystemGroups and messing with the PlayerLoop on a frequent
    /// basis. Ex: Per frame. The <see cref="WorldCache"/> may not update properly and you will have to update manually
    /// via <see cref="WorldCache.ForceRebuild"/>
    ///
    /// - If you are using <see cref="EntityQuery"/>s that your Systems aren't aware of.
    /// Ex. Queries created with <see cref="EntityManager.CreateEntityQuery"/> will not detect jobs that touch your
    /// <see cref="IBufferElementData"/>. Always use the <see cref="SystemBase.GetEntityQuery"/> function.
    ///
    /// - If you are scheduling jobs outside of a System that operate on <see cref="IBufferElementData"/> this class
    /// will not be aware of it and may lead to a dependency conflict. Consider using a
    /// <see cref="CollectionAccessController{TContext}"/> to handle this instead.
    ///
    /// - If you are scheduling multiple jobs in a System that operates on the <see cref="IBufferElementData"/> where
    /// one uses the Shared Write handle and the other(s) try and do an exclusive write or read, we treat that
    /// System as only using the Shared Write handle.
    ///
    /// - If you are calling <see cref="SystemBase.GetEntityQuery"/> that operates on the
    /// <see cref="IBufferElementData"/> outside of your <see cref="SystemBase.OnCreate"/>, this
    /// class may not be aware of the query until the <see cref="WorldCache"/> is rebuilt.
    /// This means that a write or read operation may sneak in between two shared writes which would lead to a
    /// dependency conflict. It is best practice to create all queries in the <see cref="SystemBase.OnCreate"/> or
    /// if you must create the query later, manually rebuild the <see cref="WorldCache"/>.
    ///
    /// - You may get a false positive if you have a System that has two or more queries in it. One that operates on 
    /// your <see cref="IBufferElementData"/> (QueryA) and one that doesn't (QueryB). If the logic has the QueryB
    /// execute but QueryA doesn't, we still see the system as having executed and we'll count it as a point to move
    /// the handle up when in reality it could have been ignored. 
    /// 
    /// </summary>
    /// <remarks>
    /// This is similar to the <see cref="CollectionAccessController{TContext}"/> but for specific use with a
    /// <see cref="DynamicBuffer{T}"/> where shared writing is desired.
    /// <seealso cref="DynamicBufferSharedWriteManager"/>
    /// </remarks>
    /// <typeparam name="T">The <see cref="IBufferElementData"/> type this instance is associated with.</typeparam>
    public class DynamicBufferSharedWriteHandle<T> : AbstractAnvilBase,
                                                     DynamicBufferSharedWriteManager.IDynamicBufferSharedWriteHandle
        where T : IBufferElementData
    {
        //*************************************************************************************************************
        // INTERNAL HELPER
        //*************************************************************************************************************

        /// <summary>
        /// Handles our specific cached view of the <see cref="World"/>
        /// </summary>
        private class LocalCache : AbstractCache
        {
            private readonly WorldCache m_WorldCache;
            private readonly HashSet<ComponentType> m_QueryComponentTypes;
            private readonly List<ComponentSystemBase> m_OrderedSystems = new List<ComponentSystemBase>();
            private readonly List<ComponentSystemBase> m_ExecutedOrderedSystems = new List<ComponentSystemBase>();
            private readonly Dictionary<ComponentSystemBase, int> m_ExecutedOrderedSystemsLookup = new Dictionary<ComponentSystemBase, int>();
            private readonly Dictionary<ComponentSystemBase, uint> m_OrderedSystemsVersions = new Dictionary<ComponentSystemBase, uint>();

            private int m_OrderedSystemsIndexForExecution;
            private int m_LastRebuildCheckFrameCount;

            internal LocalCache(WorldCache worldCache,
                                ComponentType componentType)
            {
                m_WorldCache = worldCache;
                m_QueryComponentTypes = new HashSet<ComponentType>
                {
                    componentType
                };
            }

            internal int GetExecutionOrderOf(ComponentSystemBase callingSystem)
            {
                return !m_ExecutedOrderedSystemsLookup.TryGetValue(callingSystem, out int order)
                    ? m_ExecutedOrderedSystems.Count
                    : order;
            }

            internal ComponentSystemBase GetSystemAtExecutionOrder(int executionOrder)
            {
                Debug.Assert(executionOrder >= 0 && executionOrder < m_ExecutedOrderedSystems.Count, $"Invalid execution order of {executionOrder}.{nameof(m_ExecutedOrderedSystems)} Count is {m_ExecutedOrderedSystems.Count}");
                return m_ExecutedOrderedSystems[executionOrder];
            }

            internal void RebuildIfNeeded()
            {
                //TODO: #27 Move to AbstractCache?
                //This might be called many times a frame by many different callers.
                //We only want to do this check once per frame.
                int currentFrameCount = Time.frameCount;
                if (m_LastRebuildCheckFrameCount == currentFrameCount)
                {
                    return;
                }

                m_LastRebuildCheckFrameCount = currentFrameCount;

                //Once per frame we want to reset our execution order since some systems may not have executed.
                ResetExecutionOrder();

                //Rebuild the world cache if it needs to be
                m_WorldCache.RebuildIfNeeded();

                //If our local cache doesn't match the latest world cache, we need to update
                if (Version == m_WorldCache.Version)
                {
                    return;
                }

                //Find all the systems that have queries that match our IBufferElementData
                RebuildMatchingSystems();

                //Ensure we're not going to do this again until the World changes.
                Version = m_WorldCache.Version;
            }

            private void RebuildMatchingSystems()
            {
                //Build up our internal model of a list (in order) of systems that will operate on our IBufferElementData
                m_OrderedSystemsVersions.Clear();
                m_WorldCache.RefreshSystemsWithQueriesFor(m_QueryComponentTypes, m_OrderedSystems);
                //Initialize a lookup with the last version these systems ran at
                foreach (ComponentSystemBase system in m_OrderedSystems)
                {
                    m_OrderedSystemsVersions[system] = system.LastSystemVersion;
                }
            }

            private void ResetExecutionOrder()
            {
                m_OrderedSystemsIndexForExecution = 0;
                m_ExecutedOrderedSystems.Clear();
                m_ExecutedOrderedSystemsLookup.Clear();
            }

            public void UpdateExecutedSystems(ComponentSystemBase callingSystem)
            {
                //Once a frame we'll end up iterating through all the OrderedSystems but there's no need to iterate
                //the whole list each time. Instead, we'll track the progress through the frame and only iterate up 
                //to the system that is currently executing and thus checking when it can shared write.
                for (; m_OrderedSystemsIndexForExecution < m_OrderedSystems.Count; ++m_OrderedSystemsIndexForExecution)
                {
                    ComponentSystemBase system = m_OrderedSystems[m_OrderedSystemsIndexForExecution];

                    //If we're the calling system, the loop is done, we should exit.
                    if (callingSystem == system)
                    {
                        break;
                    }

                    //Internally, Systems will execute if they are enabled, set to AlwaysUpdate or have any queries
                    //that will return entities. The systems do this check via ShouldRunSystem and Enabled.
                    //While we could reflect and call that again, it seems inefficient to do so especially since it 
                    //has already been done. Instead we can check if a system HAS run this frame by comparing the
                    //LastSystemVersion with our cached version. If the versions are the same, the system didn't run
                    //for any number of reasons and we can exclude it from our order. If it is enabled the next frame
                    //the versions won't match and we'll add it back to our executed list.
                    if (!DidSystemExecuteSinceLastCheck(system))
                    {
                        continue;
                    }

                    m_OrderedSystemsVersions[system] = system.LastSystemVersion;
                    m_ExecutedOrderedSystems.Add(system);
                    m_ExecutedOrderedSystemsLookup[system] = m_OrderedSystemsIndexForExecution;
                }
            }

            private bool DidSystemExecuteSinceLastCheck(ComponentSystemBase system)
            {
                uint cachedSystemVersion = m_OrderedSystemsVersions[system];
                Debug.Assert(cachedSystemVersion <= system.LastSystemVersion, $"Investigate. Cached System Version {cachedSystemVersion} is larger than the last recorded version {system.LastSystemVersion}");
                return system.LastSystemVersion > cachedSystemVersion;
            }
        }


        //*************************************************************************************************************
        // PUBLIC CLASS
        //*************************************************************************************************************

        private readonly HashSet<ComponentSystemBase> m_SharedWriteSystems = new HashSet<ComponentSystemBase>();

        private readonly World m_World;
        private readonly WorldCache m_WorldCache;
        private readonly LocalCache m_LocalCache;
        private readonly DynamicBufferSharedWriteManager.LookupByComponentType m_LookupByComponentType;

        private JobHandle m_SharedWriteDependency;

        /// <summary>
        /// The <see cref="ComponentType"/> of <see cref="IBufferElementData"/> this instance is associated with.
        /// </summary>
        public ComponentType ComponentType
        {
            get;
        }

        internal DynamicBufferSharedWriteHandle(ComponentType type,
                                                World world,
                                                DynamicBufferSharedWriteManager.LookupByComponentType lookupByComponentType)
        {
            ComponentType = type;
            m_World = world;
            m_LookupByComponentType = lookupByComponentType;
            m_WorldCache = WorldCacheManager.GetOrCreate(m_World);
            m_LocalCache = new LocalCache(m_WorldCache, ComponentType);
        }

        protected override void DisposeSelf()
        {
            // NOTE: If these asserts trigger we should think about calling Complete() on these job handles.
            Debug.Assert(m_SharedWriteDependency.IsCompleted, "The shared write access dependency is not completed");

            //Remove ourselves from the chain
            m_LookupByComponentType.Remove<T>();

            base.DisposeSelf();
        }

        /// <summary>
        /// Registers a <see cref="ComponentSystemBase"/> as a system that will shared write to
        /// the <see cref="DynamicBuffer{T}"/>.
        /// </summary>
        /// <param name="system">The <see cref="ComponentSystemBase"/> that shared writes.</param>
        public void RegisterSystemForSharedWrite(ComponentSystemBase system)
        {
            Debug.Assert(system.World == m_World, $"System {system} is not part of the same world as this {nameof(DynamicBufferSharedWriteHandle<T>)}");
            m_SharedWriteSystems.Add(system);
        }

        /// <summary>
        /// Unregisters a <see cref="ComponentSystemBase"/> as a system that will shared write to
        /// the <see cref="DynamicBuffer{T}"/>.
        /// </summary>
        /// <param name="system">The <see cref="ComponentSystemBase"/> that shared writes.</param>
        public void UnregisterSystemForSharedWrite(ComponentSystemBase system)
        {
            Debug.Assert(system.World == m_World, $"System {system} is not part of the same world as this {nameof(DynamicBufferSharedWriteHandle<T>)}");
            m_SharedWriteSystems.Remove(system);
        }

        /// <summary>
        /// Gets a <see cref="JobHandle"/> to be used to schedule the jobs that will shared writing to the
        /// <see cref="DynamicBuffer{T}"/>.
        /// </summary>
        /// <param name="callingSystem">The <see cref="SystemBase"/> that is doing the shared writing.</param>
        /// <param name="callingSystemDependency">The incoming Dependency <see cref="JobHandle"/> for the <paramref name="callingSystem"/></param>
        /// <returns></returns>
        public JobHandle GetSharedWriteJobHandle(SystemBase callingSystem, JobHandle callingSystemDependency)
        {
            Debug.Assert(m_SharedWriteSystems.Contains(callingSystem), $"Trying to get the shared write handle but {callingSystem} hasn't been registered. Did you call {nameof(RegisterSystemForSharedWrite)}?");

            //Rebuild our local cache if we need to. Will trigger a world cache rebuild if necessary too.
            m_LocalCache.RebuildIfNeeded();

            //Ensure our local cache has the right order of systems that actually executed this frame
            m_LocalCache.UpdateExecutedSystems(callingSystem);

            //Find out when our system executed in the order
            int callingSystemOrder = m_LocalCache.GetExecutionOrderOf(callingSystem);

            //If we're the first system to go in a frame, we're the first start point for shared writing.
            if (callingSystemOrder == 0)
            {
                m_SharedWriteDependency = callingSystemDependency;
            }
            //Otherwise we want to check the system that executed before us to see what kind of lock they had 
            //on our IBufferElementData
            else
            {
                ComponentSystemBase previousSystem = m_LocalCache.GetSystemAtExecutionOrder(callingSystemOrder - 1);

                //If that system was a shared writable system, we don't want to move our dependency up so that we 
                //can also share the write. If not, we move it up.
                if (!m_SharedWriteSystems.Contains(previousSystem))
                {
                    m_SharedWriteDependency = callingSystemDependency;
                }
            }

            return m_SharedWriteDependency;
        }
    }
}
