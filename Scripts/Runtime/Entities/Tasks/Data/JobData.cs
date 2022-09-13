using Unity.Core;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    public class JobData<TInstance>
        where TInstance : unmanaged, IProxyInstance
    {
        private readonly ProxyDataStream<TInstance> m_UpdateProxyDataStream;

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

        public JobData(World world,
                       byte context,
                       ProxyDataStream<TInstance> updateProxyDataStream)
        {
            World = world;
            Context = context;
            m_UpdateProxyDataStream = updateProxyDataStream;
        }

        public DataStreamUpdater<TInstance> GetDataStreamUpdater()
        {
            DataStreamUpdater<TInstance> updater = m_UpdateProxyDataStream.CreateDataStreamUpdater(Context);
            return updater;
        }
    }
}
