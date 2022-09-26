namespace Anvil.Unity.DOTS.Entities
{
    public interface IUpdateJobConfig
    {
        public IJobConfig RequireDataStreamForUpdate(AbstractProxyDataStream dataStream);
    }
}
