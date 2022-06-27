namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// An <see cref="AbstractTaskWorkData"/> specific for use in scheduling Jobs
    /// </summary>
    public class JobTaskWorkData : AbstractTaskWorkData
    {
        internal JobTaskWorkData(AbstractTaskDriverSystem system) : base(system)
        {
        }
    }
}
