using System;
using System.Diagnostics;
using Anvil.Unity.DOTS.Data;
using Unity.Entities;
using Debug = UnityEngine.Debug;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// Extension methods that make interacting with <see cref="ManagedReference{T}"/> easier.
    /// </summary>
    public static class ManagedReferenceExtension
    {
        /// <summary>
        /// Adds an <see cref="IComponentReferencable"/> instance reference to a provided <see cref="Entity"/>
        /// </summary>
        /// <param name="entityManager">The <see cref="EntityManager"/> for the <see cref="Entity"/>.</param>
        /// <param name="entity">The <see cref="Entity"/> to add the reference too.</param>
        /// <param name="instance">The instance to reference.</param>
        /// <param name="preventOverwrite">
        /// (Default: true) When true the method will throw an exception if a <see cref="ManagedReference{T}"/> of the
        /// same type already exists on the entity.
        /// </param>
        /// <typeparam name="T">The type of the managed instance.</typeparam>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <see cref="preventOverwrite"/> is true and a <see cref="ManagedReference{T}"/> of the same type
        /// already exists on the <see cref="Entity"/>.
        /// </exception>
        public static void AddManagedRefTo<T>(this EntityManager entityManager, Entity entity, T instance, bool preventOverwrite = true) where T : class, IComponentReferencable
        {
            if(preventOverwrite && entityManager.HasComponent<ManagedReference<T>>(entity))
            {
                throw new InvalidOperationException($"Managed ref for {nameof(T)} already exists in {nameof(World)}:{entityManager.World.Name} on {nameof(Entity)}:{entity}. Set {nameof(preventOverwrite)} to false to allow overwriting.");
            }

            entityManager.AddComponentData(entity, instance.AsComponentDataReference());
        }

        /// <summary>
        /// Removes an <see cref="IComponentReferencable"/> instance reference from a provided <see cref="Entity"/>
        /// </summary>
        /// <param name="entityManager">The <see cref="EntityManager"/> for the <see cref="Entity"/>.</param>
        /// <param name="entity">The <see cref="Entity"/> to remove the reference from.</param>
        /// <param name="instance">
        /// The instance to remove.
        /// Only used when DEBUG is enabled to verify the intended instance is the one being removed from the entity
        /// since this method will remove any instance of the type <see cref="T"/> from the <see cref="Entity"/>.
        /// </param>
        /// <typeparam name="T">The type of the managed instance.</typeparam>
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

        /// <summary>
        /// Requires that a singleton <see cref="IComponentReferencable"/> instance reference exists for a
        /// <see cref="ComponentSystemBase"/> to update.
        /// This is the <see cref="IComponentReferencable"/> equivalent of
        /// <see cref="ComponentSystemBase.RequireSingletonForUpdate"/>.
        /// </summary>
        /// <param name="system">The system to add the requirement too.</param>
        /// <typeparam name="T">The type of the managed instance.</typeparam>
        public static void RequireManagedSingletonForUpdate<T>(this ComponentSystemBase system) where T : class, IComponentReferencable
        {
            system.RequireSingletonForUpdate<ManagedReference<T>>();
        }

        /// <summary>
        /// Gets the singleton managed instance.
        /// This is the <see cref="IComponentReferencable"/> equivalent of
        /// <see cref="ComponentSystemBase.GetSingleton{T}"/>
        /// </summary>
        /// <param name="system">The system to query though.</param>
        /// <typeparam name="T">The type of the managed instance.</typeparam>
        /// <returns>The managed instance.</returns>
        public static T GetManagedSingleton<T>(this ComponentSystemBase system) where T : class, IComponentReferencable
        {
            return system.GetSingleton<ManagedReference<T>>().Resolve();
        }

        /// <summary>
        /// Checks whether a singleton component of the specified managed type exists.
        /// This is the <see cref="IComponentReferencable"/> equivalent of <see cref="ComponentSystemBase.HasSingleton{T}"/>
        /// </summary>
        /// <param name="system">The system to query though.</param>
        /// <typeparam name="T">The type of the managed instance.</typeparam>
        /// <returns>True if a singleton of the managed type exists.</returns>
        public static bool HasManagedSingleton<T>(this ComponentSystemBase system) where T : class, IComponentReferencable
        {
            return system.HasSingleton<ManagedReference<T>>();
        }
    }
}