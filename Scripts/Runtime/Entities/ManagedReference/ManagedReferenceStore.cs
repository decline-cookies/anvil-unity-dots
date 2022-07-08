using System.Collections.Generic;
using UnityEngine;

namespace Anvil.Unity.DOTS.Data
{
    public static class ManagedReferenceStore
    {
        private static readonly Dictionary<int, IComponentReferencable> m_instances = new Dictionary<int, IComponentReferencable>();

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            m_instances.Clear();
        }
#endif

        public static void RegisterReference(IComponentReferencable instance)
        {
            Debug.Assert(instance.GetType().IsClass, $"Implementations of {nameof(IComponentReferencable)} must be classes");
            m_instances.Add(instance.GetHashCode(), instance);
        }

        public static bool UnregisterReference(IComponentReferencable instance)
        {
            return m_instances.Remove(instance.GetHashCode());
        }

        internal static int GetHash(IComponentReferencable instance)
        {
            int hashCode = instance.GetHashCode();
            Debug.Assert(m_instances.ContainsKey(hashCode),
                $"Instance is not yet registered with {nameof(ManagedReferenceStore)}. Make sure it is registering on creation and unregistering on disposal before using.");

            return hashCode;
        }

        public static T Get<T>(int managedReferenceHash) where T : class, IComponentReferencable
        {
            return (T)m_instances[managedReferenceHash];
        }
    }
}