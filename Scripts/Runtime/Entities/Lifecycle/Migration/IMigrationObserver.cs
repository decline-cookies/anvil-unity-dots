using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// Implement and register with <see cref="WorldEntityMigrationSystem"/> to receive a notification when
    /// Entities are being migrated from one <see cref="World"/> to another. This will allow for scheduling jobs to
    /// handle any custom migration for data that refers to <see cref="Entity"/>s but is not automatically handled by
    /// Unity.
    /// NOTE: The jobs that are scheduled will be completed immediately, but this allows for taking advantage of
    /// multiple cores. 
    /// </summary>
    public interface IMigrationObserver
    {
        /// <summary>
        /// Implement to handle any custom migration work.
        /// </summary>
        /// <param name="dependsOn">The <see cref="JobHandle"/> to wait on.</param>
        /// <param name="destinationWorld">The <see cref="World"/> the entities are moving to.</param>
        /// <param name="remapArray">The remapping array for Entities that were in this world and are moving to
        /// the next World. See <see cref="EntityRemapUtility.EntityRemapInfo"/> and <see cref="EntityRemapUtility"/>
        /// for more details if custom usage is needed.</param>
        /// <returns>The <see cref="JobHandle"/> that represents all the custom migration work to do.</returns>
        public JobHandle MigrateTo(JobHandle dependsOn, World destinationWorld, ref NativeArray<EntityRemapUtility.EntityRemapInfo> remapArray);
    }
}
