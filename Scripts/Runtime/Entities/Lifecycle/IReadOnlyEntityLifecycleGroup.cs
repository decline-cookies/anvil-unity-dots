using Anvil.Unity.DOTS.Jobs;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// Provides access to Entities that were created or destroyed this frame.
    /// </summary>
    /// <remarks>
    /// This applies to both Entities that were created or destroyed explicitly or that have been imported/evicted
    /// to the world. Ex. Entities that move from World A to World B will count as destruction in World B and
    /// Creation in World A.
    /// </remarks>
    public interface IReadOnlyEntityLifecycleGroup
    {
        /// <summary>
        /// Gets access to the read only list of created entities for this frame for use in a job.
        /// </summary>
        /// <param name="createdEntities">The list of entities created or imported this frame.</param>
        /// <returns>The <see cref="JobHandle"/> dependency to wait on</returns>
        public JobHandle AcquireCreationAsync(out UnsafeList<Entity> createdEntities);
        
        /// <summary>
        /// Lets the underlying collection know when the jobs that were accessing it will be complete.
        /// </summary>
        /// <param name="dependsOn">
        /// The <see cref="JobHandle"/> that signifies the end of interaction with the
        /// collection.
        /// </param>
        public void ReleaseCreationAsync(JobHandle dependsOn);
        
        /// <summary>
        /// Gets access to the read only list of created or imported entities for this frame for use on the main thread.
        /// </summary>
        /// <returns>An AccessHandle to interact with.</returns>
        public AccessControlledValue<UnsafeList<Entity>>.AccessHandle AcquireCreation();
        
        /// <summary>
        /// Gets access to the read only list of destroyed entities for this frame for use in a job.
        /// </summary>
        /// <param name="destroyedEntities">The list of entities destroyed or evicted this frame.</param>
        /// <returns>The <see cref="JobHandle"/> dependency to wait on</returns>
        public JobHandle AcquireDestructionAsync(out UnsafeList<Entity> destroyedEntities);
        
        /// <summary>
        /// Lets the underlying collection know when the jobs that were accessing it will be complete.
        /// </summary>
        /// <param name="dependsOn">
        /// The <see cref="JobHandle"/> that signifies the end of interaction with the
        /// collection.
        /// </param>
        public void ReleaseDestructionAsync(JobHandle dependsOn);
        
        /// <summary>
        /// Gets access to the read only list of destroyed or evicted entities for this frame for use on the main thread.
        /// </summary>
        /// <returns>An AccessHandle to interact with.</returns>
        public AccessControlledValue<UnsafeList<Entity>>.AccessHandle AcquireDestruction();
    }
}
