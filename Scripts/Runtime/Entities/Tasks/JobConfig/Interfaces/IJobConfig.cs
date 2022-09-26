namespace Anvil.Unity.DOTS.Entities
{
    public interface IJobConfig
    {
        public bool IsEnabled { get; set; }

        public IJobConfig RunOnce();
    }
}
