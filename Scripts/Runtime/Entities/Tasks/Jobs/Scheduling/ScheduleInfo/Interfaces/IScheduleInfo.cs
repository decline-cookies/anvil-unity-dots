namespace Anvil.Unity.DOTS.Entities.Tasks
{
    public interface IScheduleInfo : IScheduleInfoBase
    {
        /// <summary>
        /// The total number of all elements
        /// </summary>
        public int Length { get; }
    }
}
