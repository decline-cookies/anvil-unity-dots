using System;
using System.Diagnostics;
using Anvil.Unity.DOTS.Data;
using Unity.Entities;
using Debug = UnityEngine.Debug;

namespace Anvil.Unity.DOTS.Entities
{
    public static class ManagedReferenceExtension
    {
        public static void AddManagedRefTo<T>(this EntityManager entityManager, Entity entity, T instance, bool preventOverwrite = true) where T : class, IComponentReferencable
        {
            if(preventOverwrite && entityManager.HasComponent<ManagedReference<T>>(entity))
            {
                throw new InvalidOperationException($"Managed ref for {nameof(T)} already exists in {nameof(World)}:{entityManager.World.Name} on {nameof(Entity)}:{entity}. Set {nameof(preventOverwrite)} to false to allow overwriting.");
            }

            entityManager.AddComponentData(entity, instance.AsComponentDataReference());
        }

        public static void RemoveManagedRefFrom<T>(this EntityManager entityManager, Entity entity, T instance) where T : class, IComponentReferencable
        {
            DEBUG_AssertInstanceMatchesRef(entityManager, entity, instance);
            entityManager.RemoveComponent<ManagedReference<T>>(entity);
        }

        // Ensure that the instance that's been provided matches the one defined in the component ref on the entity
        [Conditional("DEBUG")]
        private static void DEBUG_AssertInstanceMatchesRef<T>(EntityManager entityManager, Entity entity, T instance) where T : class, IComponentReferencable
        {
            ManagedReference<T> refComponent = entityManager.GetComponentData<ManagedReference<T>>(entity);
            Debug.Assert(refComponent.Resolve() == instance);
        }
    }
}