using Unity.Core;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    public class UpdateJobData<TData>
        where TData : unmanaged, IProxyData
    {
        private readonly ProxyDataStream<TData> m_UpdateProxyDataStream;

        public World World
        {
            get;
        }

        public ref readonly TimeData Time
        {
            get => ref World.Time;
        }

        public byte Context
        {
            get;
        }

        public UpdateJobData(World world, 
                             byte context, 
                             ProxyDataStream<TData> updateProxyDataStream)
        {
            World = world;
            Context = context;
            m_UpdateProxyDataStream = updateProxyDataStream;
        }

        public DataStreamUpdater<TData> GetDataStreamUpdater()
        {
            DataStreamUpdater<TData> updater = m_UpdateProxyDataStream.CreateDataStreamUpdater(Context);
            return updater;
        }
    }
}
