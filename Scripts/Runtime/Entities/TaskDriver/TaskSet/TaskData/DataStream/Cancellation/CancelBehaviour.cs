namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    public enum CancelBehaviour
    {
        /// <summary>
        /// When a cancellation request is received, the data is removed from the
        /// <see cref="IAbstractDataStream"/> and is considered cancelled during the
        /// consolidation phase.
        /// </summary>
        /// <remarks>
        /// This is the most common case and should be used for data that does not need
        /// to gracefully unwind when cancelled. 
        /// </remarks>
        Default,
        /// <summary>
        /// When a cancellation request is received, the data remains in the
        /// <see cref="IAbstractDataStream"/> and cannot be cancelled. The data will
        /// survive one iteration of the consolidation phase.
        /// </summary>
        /// <remarks>
        /// A rare case but used in situations where custom cancel result data is needed.
        /// For example, a Timer that when cancelled wants to communicate how much time was left
        /// on the Timer. 
        /// </remarks>
        None,
        /// <summary>
        /// When a cancellation request is received, the data is removed from the
        /// <see cref="IAbstractDataStream"/> and moved to a hidden internal
        /// <see cref="IAbstractDataStream"/> during the consolidation phase.
        /// </summary>
        /// <remarks>
        /// Instead of being deleted like in <see cref="Default"/>
        /// it is copied to a hidden internal <see cref="IAbstractDataStream"/> that
        /// can be used to explicitly cancel. This allows for custom jobs to run on
        /// cancelled data to gracefully unwind.
        /// </remarks>
        Explicit
    }
}
