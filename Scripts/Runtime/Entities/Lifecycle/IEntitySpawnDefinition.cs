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
    /// The first is a <code>public static readonly ComponentType[] COMPONENTS</code>
    /// field to define the structure of the <see cref="Entity"/> with the different
    /// components needed. The <see cref="EntitySpawnSystem"/> will enforce this via runtime
    /// checks and give helpful error messages.
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
    /// public void PopulateOnEntity(Entity entity, ref EntityCommandBuffer ecb)
    /// {
    ///     ecb.SetComponent(entity, new ActorName(m_ActorName));
    ///     ecb.SetSharedComponent(entity, new EnvironmentMembershipTag(m_EnvironmentID));
    ///     ecb.SetComponent(entity, new ActorGridPosition(m_InitialGridPosition));
    ///     ecb.SetComponent(entity, new ActorGridRotation(m_InitialGridRotation));
    /// }
    /// </example>
    /// Similarly, inheritance and composition can be achieved by triggering the <see cref="PopulateOnEntity"/>
    /// function on child <see cref="IEntitySpawnDefinition"/>s contained within the
    /// parent <see cref="IEntitySpawnDefinition"/>
    /// <example>
    /// public void PopulateOnEntity(Entity entity, ref EntityCommandBuffer ecb)
    /// {
    ///     m_BaseActorDefinition.PopulateOnEntity(entity, ref ecb);
    /// }
    /// </example>
    /// </remarks>
    public interface IEntitySpawnDefinition
    {
        /// <summary>
        /// Called automatically when spawning to populate a newly created <see cref="Entity"/>
        /// with the data needed.
        /// </summary>
        /// <param name="entity">The newly created <see cref="Entity"/></param>
        /// <param name="ecb">
        /// The <see cref="EntityCommandBuffer"/> that will apply the data.
        /// </param>
        public void PopulateOnEntity(Entity entity, ref EntityCommandBuffer ecb);
    }
}
