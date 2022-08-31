using Unity.Collections;

namespace Anvil.Unity.DOTS.Data
{
    //TODO: DOCS
    [BurstCompatible]
    public readonly struct VDResultsDestination<TTaskResultData>
        where TTaskResultData : unmanaged
    {
        internal static unsafe VDResultsDestination<TTaskResultData> ReinterpretFromPointer(void* ptr)
        {
            UnsafeTypedStream<TTaskResultData>.Writer resultWriter = UnsafeTypedStream<TTaskResultData>.Writer.ReinterpretFromPointer(ptr);
            return new VDResultsDestination<TTaskResultData>(resultWriter);
        }

        [ReadOnly] private readonly UnsafeTypedStream<TTaskResultData>.Writer m_ResultWriter;

        internal VDResultsDestination(UnsafeTypedStream<TTaskResultData>.Writer resultWriter)
        {
            m_ResultWriter = resultWriter;
        }

        //Called internally when we're sure we have access to actually write
        internal VDResultsWriter<TTaskResultData> AsResultsWriter()
        {
            return new VDResultsWriter<TTaskResultData>(m_ResultWriter);
        }
    }
}
