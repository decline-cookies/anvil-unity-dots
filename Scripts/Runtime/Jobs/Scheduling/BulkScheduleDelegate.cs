using Unity.Jobs;

namespace Anvil.Unity.DOTS.Jobs
{
    /// <summary>
    /// A function signature for use with <see cref="BulkSchedulingExtension"/> to schedule many jobs
    /// in parallel or in sequence and chaining the dependencies properly.
    /// This signature is that of an "Open Instance Delegate" which allows for an instance method to be called
    /// on a collection of instances rather than being tied to a specific instance.
    /// See <see cref="BulkSchedulingUtil"/> to create this delegate easily.
    /// </summary>
    /// <typeparam name="T">The type this function will be found on.</typeparam>
    public delegate JobHandle BulkScheduleDelegate<in T>(T element, JobHandle dependsOn);
}
