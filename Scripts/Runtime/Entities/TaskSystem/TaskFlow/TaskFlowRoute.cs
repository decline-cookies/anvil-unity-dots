namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal enum TaskFlowRoute
    {
        /// <summary>
        /// Used to allow task drivers to write to populate system data
        /// </summary>
        Populate,
        /// <summary>
        /// Used to update the owned data of task drivers or systems so they can be processed or resolved.
        /// </summary>
        Update,
        /// <summary>
        /// Used to cancel the owned data of task drivers or systems so custom unwinding can happen.
        /// </summary>
        Cancel
    }
}
