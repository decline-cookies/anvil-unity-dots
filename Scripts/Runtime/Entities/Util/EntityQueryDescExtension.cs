using System.Linq;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// A collection of extension methods for <see cref="EntityQueryDesc"/>.
    /// </summary>
    public static class EntityQueryDescExtension
    {
        /// <summary>
        /// Merge two <see cref="EntityQueryDesc"/> instances into a new instance by concatenating component lists and
        /// OR merging the <see cref="EntityQueryOptions"/>.
        /// </summary>
        /// <param name="desc1">A query description to merge.</param>
        /// <param name="desc2">A query description to merge.</param>
        /// <returns>The merged query description (new instance).</returns>
        public static EntityQueryDesc Concat(this EntityQueryDesc desc1, EntityQueryDesc desc2)
        {
            return new EntityQueryDesc()
            {
                All = desc1.All.Concat(desc2.All).ToReadOnly().ToArray(),
                Any = desc1.Any.Concat(desc2.Any).ToReadOnly().ToArray(),
                None = desc1.None.Concat(desc2.None).ToReadOnly().ToArray(),
                Options = desc1.Options | desc2.Options
            };
        }
    }
}