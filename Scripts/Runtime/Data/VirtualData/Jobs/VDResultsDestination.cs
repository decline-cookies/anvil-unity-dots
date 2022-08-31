using Unity.Collections;

namespace Anvil.Unity.DOTS.Data
{
    /// <summary>
    /// Represents a reference to <see cref="VirtualData{TKey,TInstance}"/> where results
    /// will be written to at a later time.
    /// No reading or writing can happen until that later time when the proper access will have
    /// been resolved.
    /// Use with <see cref="ITaskData{TEnum}"/>
    /// </summary>
    /// <typeparam name="TTaskResultData">The type of result that can be written</typeparam>
    [BurstCompatible]
    public readonly struct VDResultsDestination<TTaskResultData>
        where TTaskResultData : unmanaged
    {
        internal static unsafe VDResultsDestination<TTaskResultData> CreateFromPointer(void* ptr)
        {
            UnsafeTypedStream<TTaskResultData>.Writer resultWriter = UnsafeTypedStream<TTaskResultData>.Writer.CreateFromPointer(ptr);
            return new VDResultsDestination<TTaskResultData>(resultWriter);
        }

        [ReadOnly] private readonly UnsafeTypedStream<TTaskResultData>.Writer m_ResultWriter;

        internal VDResultsDestination(UnsafeTypedStream<TTaskResultData>.Writer resultWriter)
        {
            m_ResultWriter = resultWriter;
        }

        internal unsafe void* GetPointer()
        {
            return m_ResultWriter.GetPointer();
        }
        
        //Called internally when we're sure we have access to actually write
        internal VDResultsWriter<TTaskResultData> AsResultsWriter()
        {
            return new VDResultsWriter<TTaskResultData>(m_ResultWriter);
        }
    }
}
