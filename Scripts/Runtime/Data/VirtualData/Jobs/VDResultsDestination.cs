using Unity.Collections;

namespace Anvil.Unity.DOTS.Data
{
    //TODO: DOCS
    [BurstCompatible]
    public readonly struct VDResultsDestination<TTaskResultData>
        where TTaskResultData : unmanaged, IEntityProxyData
    {
        internal static unsafe VDResultsDestination<TTaskResultData> ReinterpretFromPointer(void* ptr)
        {
            UnsafeTypedStream<VDInstanceWrapper<TTaskResultData>>.Writer resultWriter = UnsafeTypedStream<VDInstanceWrapper<TTaskResultData>>.Writer.ReinterpretFromPointer(ptr);
            return new VDResultsDestination<TTaskResultData>(resultWriter);
        }

        [ReadOnly] private readonly UnsafeTypedStream<VDInstanceWrapper<TTaskResultData>>.Writer m_ResultWriter;

        internal VDResultsDestination(UnsafeTypedStream<VDInstanceWrapper<TTaskResultData>>.Writer resultWriter)
        {
            m_ResultWriter = resultWriter;
        }

        //Called internally when we're sure we have access to actually write
        internal VDResultsWriter<TTaskResultData> AsResultsWriter(uint context)
        {
            return new VDResultsWriter<TTaskResultData>(m_ResultWriter, context);
        }
    }
}
