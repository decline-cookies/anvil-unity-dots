namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    /// <summary>
    /// Specific scheduling information for a <see cref="CancelCompleteJobConfig"/>
    /// </summary>
    public class CancelCompleteScheduleInfo : DataStreamScheduleInfo<CancelComplete>
    {
        internal CancelCompleteScheduleInfo(
            CancelCompleteJobData jobData,
            CancelCompleteDataStream cancelCompleteDataStream,
            BatchStrategy batchStrategy,
            JobConfigScheduleDelegates.ScheduleDataStreamJobDelegate<CancelComplete> scheduleJobFunction)
            : base(jobData, cancelCompleteDataStream, batchStrategy, scheduleJobFunction) { }
    }
}
