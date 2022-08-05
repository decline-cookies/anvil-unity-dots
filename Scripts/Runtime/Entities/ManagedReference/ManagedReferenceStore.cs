using System.Collections.Generic;
using UnityEngine;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// A static store of all <see cref="IComponentReferencable"/> instances. Used to get instances by hash.
    /// Used by <see cref="ManagedReference{T}"/>.
    /// </summary>
    public static class ManagedReferenceStore
    {
        private static readonly Dictionary<int, IComponentReferencable> m_Instances = new Dictionary<int, IComponentReferencable>();

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            m_Instances.Clear();
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
            m_Instances.Add(instance.GetHashCode(), instance);
        }

        /// <summary>
        /// Unregisters a managed instance.
        /// This is usually called when the instance is destroyed.
        /// </summary>
        /// <param name="instance">The instance to unregister.</param>
        /// <returns>True if the instance is successfully found and removed.</returns>
        public static bool UnregisterReference(IComponentReferencable instance)
        {
            return m_Instances.Remove(instance.GetHashCode());
        }

        /// <summary>
        /// Gets the hash of an <see cref="IComponentReferencable"/> instance.
        /// </summary>
        /// <param name="instance">The instance to get the hash of.</param>
        /// <returns>The hash of the instance.</returns>
        internal static int GetHash(IComponentReferencable instance)
        {
            int hashCode = instance.GetHashCode();
            Debug.Assert(m_Instances.ContainsKey(hashCode),
                $"Instance is not yet registered with {nameof(ManagedReferenceStore)}. Make sure it is registering on creation and unregistering on disposal before using.");

            return hashCode;
        }

        /// <summary>
        /// Get a managed instance by its hash.
        /// </summary>
        /// <param name="managedReferenceHash">
        /// The hash of the managed instance.
        /// Usually provided by a <see cref="ManagedReference{T}"/>.
        /// </param>
        /// <typeparam name="T">The type of the managed instance.</typeparam>
        /// <returns>The managed instance.</returns>
        public static T Get<T>(int managedReferenceHash) where T : class, IComponentReferencable
        {
            return (T)m_Instances[managedReferenceHash];
        }
    }
}