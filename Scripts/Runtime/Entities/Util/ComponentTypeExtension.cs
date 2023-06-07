using System.Collections.Generic;
using System.Linq;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// Helper methods when dealing with <see cref="ComponentTypes"/>
    /// </summary>
    public static class ComponentTypeExtension
    {
        /// <summary>
        /// Converts an array of <see cref="ComponentType"/>s to be readonly.
        /// </summary>
        /// <remarks>
        /// A component array created using typeof(ComponentType) will default all created <see cref="ComponentType"/>
        /// to be readwrite. When generating a query from that array, the query access will be expensive since
        /// Unity will think that all the components will be written to. This is useful for making a query
        /// that uses all of those components but in a readonly context to limit blocking.
        /// </remarks>
        /// <param name="componentTypes">The array of <see cref="ComponentType"/> to convert</param>
        /// <returns>An array of <see cref="ComponentType"/> that are all readonly</returns>
        public static ComponentType[] ToReadOnly(this ComponentType[] componentTypes)
        {
            ComponentType[] readOnlyTypes = new ComponentType[componentTypes.Length];
            for (int i = 0; i < componentTypes.Length; ++i)
            {
                readOnlyTypes[i] = ComponentType.ReadOnly(componentTypes[i].TypeIndex);
            }

            return readOnlyTypes;
        }

        /// <summary>
        /// Converts an <see cref="IEnumerable{ComponentType}"/> to be readonly.
        /// </summary>
        /// <remarks>
        /// A component collection created using typeof(ComponentType) will default all created <see cref="ComponentType"/>
        /// to be readwrite. When generating a query from that array, the query access will be expensive since
        /// Unity will think that all the components will be written to. This is useful for making a query
        /// that uses all of those components but in a readonly context to limit blocking.
        /// </remarks>
        /// <param name="componentTypes">The array of <see cref="ComponentType"/> to convert.</param>
        /// <returns>An <see cref="IEnumerable{ComponentType}"/> that are all readonly.</returns>
        public static IEnumerable<ComponentType> ToReadOnly(this IEnumerable<ComponentType> componentTypes)
        {
            return componentTypes
                .Select(type => ComponentType.ReadOnly(type.TypeIndex));
        }
    }
}