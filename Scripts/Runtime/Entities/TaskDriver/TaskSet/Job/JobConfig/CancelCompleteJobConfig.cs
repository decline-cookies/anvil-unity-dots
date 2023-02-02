using Anvil.Unity.DOTS.Jobs;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal class CancelCompleteJobConfig : DataStreamJobConfig<CancelComplete>
    {
        public CancelCompleteJobConfig(ITaskSetOwner taskSetOwner,
                                       CancelCompleteDataStream cancelCompleteDataStream)
            : base(taskSetOwner)
        {
            AddAccessWrapper(new CancelCompleteActiveAccessWrapper(cancelCompleteDataStream, AccessType.SharedRead, Usage.CancelComplete));
        }
    }
}
