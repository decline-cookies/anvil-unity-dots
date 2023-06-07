using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// Instructions and data for how to build an <see cref="Entity"/> and populate it
    /// with data when spawning.
    /// </summary>
    /// <remarks>
    /// There are two parts to an Entity Spawn Definition.
    ///
    /// The first is the <see cref="RequiredComponents"/> function that returns the structure of
    /// the <see cref="Entity"/> with the different components needed. Best practice is to define via a
    /// static readonly array and then return that via the instance getter.
    /// <example>
    /// public static readonly ComponentType[] COMPONENTS = new ComponentType[]
    /// {
    ///     typeof(ActorTag), 
    ///     typeof(ActorName), 
    ///     typeof(EnvironmentMembershipTag), 
    ///     typeof(ActorGridPosition), 
    ///     typeof(ActorGridRotation), 
    ///     typeof(IsSelected)
    /// };
    ///
    /// public ComponentType[] RequiredComponents
    /// {
    ///     get => COMPONENTS;
    /// }
    /// </example>
    /// 
    /// Inheritance and composition can be achieved by mixing different
    /// <see cref="IEntitySpawnDefinition"/>s COMPONENTS fields in.
    /// <example>
    /// public static readonly ComponentType[] COMPONENTS = new ComponentType[]
    /// {
    ///     typeof(CrewActorTag)
    /// }
    /// .Concat(BaseActorDefinition.COMPONENTS)
    /// .ToArray();
    /// </example>
    ///
    /// The second part is the function <see cref="PopulateOnEntity"/> which will pass
    /// in the <see cref="Entity"/> that has been created and the <see cref="EntityCommandBuffer"/>
    /// that will populate that Entity later on during the <see cref="EntitySpawnSystem"/>s
    /// update phase.
    /// <example>
    /// public void PopulateOnEntity(Entity entity, in EntitySpawner entitySpawner)
    /// {
    ///     entitySpawner.SetComponent(entity, new ActorName(m_ActorName));
    ///     entitySpawner.SetSharedComponent(entity, new EnvironmentMembershipTag(m_EnvironmentID));
    ///     entitySpawner.SetComponent(entity, new ActorGridPosition(m_InitialGridPosition));
    ///     entitySpawner.SetComponent(entity, new ActorGridRotation(m_InitialGridRotation));
    /// }
    /// </example>
    /// Similarly, inheritance and composition can be achieved by triggering the <see cref="PopulateOnEntity"/>
    /// function on child <see cref="IEntitySpawnDefinition"/>s contained within the
    /// parent <see cref="IEntitySpawnDefinition"/>
    /// <example>
    /// public void PopulateOnEntity(Entity entity, in EntitySpawner entitySpawner)
    /// {
    ///     m_BaseActorDefinition.PopulateOnEntity(entity, entitySpawner);
    /// }
    /// </example>
    /// </remarks>
    public interface IEntitySpawnDefinition
    {
        /// <summary>
        /// Returns the array of required component types to added to the <see cref="Entity"/> that make up its
        /// <see cref="EntityArchetype"/>
        /// </summary>
        public ComponentType[] RequiredComponents { get; }
        
        /// <summary>
        /// Called automatically when spawning to populate a newly created <see cref="Entity"/>
        /// with the data needed.
        /// </summary>
        /// <param name="entity">The newly created <see cref="Entity"/></param>
        /// <param name="entitySpawner">The <see cref="EntitySpawner"/> helper struct</param>
        public void PopulateOnEntity(Entity entity, in EntitySpawner entitySpawner);
    }
}
