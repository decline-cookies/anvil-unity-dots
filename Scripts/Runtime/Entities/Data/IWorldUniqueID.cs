namespace Anvil.Unity.DOTS.Entities
{
    public interface IWorldUniqueID<out TWorldUniqueID>
    {
        public TWorldUniqueID WorldUniqueID { get; }
    }
}
