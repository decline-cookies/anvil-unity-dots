// using Unity.Entities;
//
// namespace Anvil.Unity.DOTS.Entities.Tasks
// {
//     /// <summary>
//     /// Triggering specific <see cref="AbstractJobData"/> for use when triggering a job based
//     /// of when a <see cref="AbstractTaskDriver"/> has completed the cancellation for an
//     /// <see cref="Entity"/>
//     /// </summary>
//     public class CancelCompleteJobData : AbstractJobData
//     {
//         private readonly CancelCompleteJobConfig m_JobConfig;
//
//         internal CancelCompleteJobData(CancelCompleteJobConfig jobConfig) : base(jobConfig)
//         {
//             m_JobConfig = jobConfig;
//         }
//
//         /// <summary>
//         /// Gets a <see cref="CancelCompleteReader"/> job-safe struct to use for reading <see cref="Entity"/>s
//         /// that have completed their cancellation.
//         /// </summary>
//         /// <returns>The <see cref="CancelCompleteReader"/></returns>
//         public CancelCompleteReader GetCancelCompleteReader()
//         {
//             CancelCompleteDataStream cancelCompleteDataStream = m_JobConfig.GetCancelCompleteDataStream(AbstractJobConfig.Usage.Read);
//             CancelCompleteReader cancelCompleteReader = cancelCompleteDataStream.CreateCancelCompleteReader();
//             return cancelCompleteReader;
//         }
//     }
// }
