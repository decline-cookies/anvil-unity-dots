using System.Collections.Generic;
using Anvil.CSharp.Data;
using Anvil.CSharp.Logging;
using UnityEngine;
using Logger = Anvil.CSharp.Logging.Logger;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// A static store of all <see cref="IComponentReferencable"/> instances. Used to get instances by ID.
    /// Used by <see cref="ManagedReference{T}"/>.
    /// </summary>
    public static class ManagedReferenceStore
    {
        private static readonly Logger s_Logger = Log.GetStaticLogger(typeof(ManagedReferenceStore));
        private static readonly Dictionary<uint, IComponentReferencable> s_IDToInstance = new Dictionary<uint, IComponentReferencable>();
        private static readonly Dictionary<IComponentReferencable, uint> s_InstanceToID = new Dictionary<IComponentReferencable, uint>();
        private static IDProvider s_IDProvider;

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            s_IDProvider = new IDProvider(uint.MaxValue - 1_000_000);
            s_IDToInstance.Clear();
            s_InstanceToID.Clear();
        }
#endif

        /// <summary>
        /// Registers a managed instance so it can be resolved by a <see cref="ManagedReference{T}"/>.
        /// This is usually called when the instance is created.
        /// </summary>
        /// <param name="instance">The instance to register.</param>
        public static void RegisterReference(IComponentReferencable instance)
        {
            Debug.Assert(instance.GetType().IsClass, $"Implementations of {nameof(IComponentReferencable)} must be classes");

            uint id = s_IDProvider.GetNextID();
            if(!s_InstanceToID.TryAdd(instance, id))
            {
                s_Logger.Warning($"Instance already registered, ignoring. instance: {instance}");
                return;
            }
            s_IDToInstance.Add(id, instance);
        }

        /// <summary>
        /// Unregisters a managed instance.
        /// This is usually called when the instance is destroyed.
        /// </summary>
        /// <param name="instance">The instance to unregister.</param>
        /// <returns>True if the instance is successfully found and removed.</returns>
        public static bool UnregisterReference(IComponentReferencable instance)
        {
            if (!s_InstanceToID.TryGetValue(instance, out uint id))
            {
                Debug.Assert(!s_IDToInstance.ContainsValue(instance));
                return false;
            }

            Debug.Assert(s_IDToInstance.ContainsKey(id));
            s_InstanceToID.Remove(instance);
            s_IDToInstance.Remove(id);
            return true;
        }

        /// <summary>
        /// Gets the ID of an <see cref="IComponentReferencable"/> instance.
        /// </summary>
        /// <param name="instance">The instance to get the ID of.</param>
        /// <returns>The ID of the instance.</returns>
        internal static uint GetID(IComponentReferencable instance)
        {
            Debug.Assert(s_InstanceToID.ContainsKey(instance),
                $"Instance is not yet registered with {nameof(ManagedReferenceStore)}. Make sure it is registering on creation and unregistering on disposal before using.");

            return s_InstanceToID[instance];
        }

        /// <summary>
        /// Get a managed instance by its ID.
        /// </summary>
        /// <param name="id">
        /// The ID of the managed instance.
        /// Usually provided by a <see cref="ManagedReference{T}"/>.
        /// </param>
        /// <typeparam name="T">The type of the managed instance.</typeparam>
        /// <returns>The managed instance.</returns>
        internal static T Get<T>(uint id) where T : class, IComponentReferencable
        {
            return (T)s_IDToInstance[id];
        }
    }
}