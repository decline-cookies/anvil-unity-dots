using Anvil.Unity.DOTS.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class CancelCompleteJobConfig : AbstractJobConfig
    {
        public CancelCompleteJobConfig(ITaskSetOwner taskSetOwner,
                                       CancelCompleteDataStream cancelCompleteDataStream)
            : base(taskSetOwner)
        {
            AddAccessWrapper(new CancelCompleteActiveAccessWrapper(cancelCompleteDataStream, AccessType.SharedRead, Usage.CancelComplete));
        }
    }
}
