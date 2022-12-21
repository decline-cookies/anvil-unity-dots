namespace Anvil.Unity.DOTS.Entities.Tasks
{
    public interface IResolvableJobConfigRequirements : IJobConfig
    {
        public IResolvableJobConfigRequirements RequireResolveTarget<TResolveTargetType>()
            where TResolveTargetType : unmanaged, IEntityProxyInstance;
    }
}
