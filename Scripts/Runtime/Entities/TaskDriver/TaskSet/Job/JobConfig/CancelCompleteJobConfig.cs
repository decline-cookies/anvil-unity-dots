using Anvil.Unity.DOTS.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class CancelCompleteJobConfig : AbstractJobConfig
    {
        public CancelCompleteJobConfig(ITaskSetOwner taskSetOwner,
                                       CancelCompleteDataStream cancelCompleteDataStream)
            : base(taskSetOwner)
        {
            //TODO: Move into a function?
            AddAccessWrapper(new CancelCompleteActiveAccessWrapper(cancelCompleteDataStream, AccessType.SharedRead, Usage.CancelComplete));
        }
    }
}
