// using Anvil.Unity.DOTS.Jobs;
// using Unity.Jobs;
//
// namespace Anvil.Unity.DOTS.Entities.Tasks
// {
//     internal class CancelFlowAccessWrapper : AbstractAccessWrapper
//     {
//         public TaskDriverCancelFlow CancelFlow { get; }
//
//         public CancelFlowAccessWrapper(TaskDriverCancelFlow cancelFlow, AccessType accessType, AbstractJobConfig.Usage usage) : base(accessType, usage)
//         {
//             CancelFlow = cancelFlow;
//         }
//
//         public sealed override JobHandle Acquire()
//         {
//             return CancelFlow.AcquireAsync(AccessType);
//         }
//
//         public sealed override void Release(JobHandle dependsOn)
//         {
//             CancelFlow.ReleaseAsync(dependsOn);
//         }
//     }
// }
