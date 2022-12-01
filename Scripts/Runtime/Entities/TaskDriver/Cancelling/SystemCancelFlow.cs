using System.Collections.Generic;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class SystemCancelFlow : AbstractCancelFlow
    {

        public SystemCancelFlow(AbstractWorkload owningWorkload, TaskDriverCancelFlow parent) : base(owningWorkload, parent)
        {
        }

        internal void BuildRelationshipData(List<CancelRequestDataStream> cancelRequests,
                                            List<byte> contexts)
        {
            //Add ourself
            cancelRequests.Add(OwningWorkload.CancelRequestDataStream);
            //Add the previous context which will represent the TaskDriver that writes to us
            contexts.Add(Parent.TaskDriverContext);
        }
    }
}
