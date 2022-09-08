//TODO: RE-ENABLE IF NEEDED
// using Unity.Collections;
//
// namespace Anvil.Unity.DOTS.Data
// {
//     //TODO: DOCS
//     [BurstCompatible]
//     public readonly struct VDResultsDestination<TTaskResultData>
//         where TTaskResultData : unmanaged, IProxyData
//     {
//         internal static unsafe VDResultsDestination<TTaskResultData> ReinterpretFromPointer(void* ptr)
//         {
//             UnsafeTypedStream<PDWrapper<TTaskResultData>>.Writer resultWriter = UnsafeTypedStream<PDWrapper<TTaskResultData>>.Writer.ReinterpretFromPointer(ptr);
//             return new VDResultsDestination<TTaskResultData>(resultWriter);
//         }
//
//         [ReadOnly] private readonly UnsafeTypedStream<PDWrapper<TTaskResultData>>.Writer m_ResultWriter;
//
//         internal VDResultsDestination(UnsafeTypedStream<PDWrapper<TTaskResultData>>.Writer resultWriter)
//         {
//             m_ResultWriter = resultWriter;
//         }
//
//         //Called internally when we're sure we have access to actually write
//         internal VDResultsWriter<TTaskResultData> AsResultsWriter(byte context)
//         {
//             return new VDResultsWriter<TTaskResultData>(m_ResultWriter, context);
//         }
//     }
// }
