using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    public static class ComponentTypeExtension
    {
        public static ComponentType[] ToReadOnly(this ComponentType[] componentTypes)
        {
            ComponentType[] readOnlyTypes = new ComponentType[componentTypes.Length];
            for (int i = 0; i < componentTypes.Length; ++i)
            {
                readOnlyTypes[i] = ComponentType.ReadOnly(componentTypes[i].TypeIndex);
            }

            return readOnlyTypes;
        }
    }
}
