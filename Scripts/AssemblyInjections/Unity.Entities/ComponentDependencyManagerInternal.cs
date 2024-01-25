using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    [BurstCompile]
    public static class ComponentDependencyManagerInternal
    {
        public const int MAX_READ_HANDLES = 17;
        public const int FIELD_OFFSET_TYPE_ARRAY_INDICES = 16;
        public const int FIELD_OFFSET_DEPENDENCY_HANDLES = 24;

        //Mimic of ComponentDependencyManager.DependencyHandle
        [BurstCompile]
        internal struct DependencyHandle
        {
            public JobHandle WriteFence;
            public int       NumReadFences;
            public TypeIndex TypeIndex;
        }

        [BurstCompile]
        internal static unsafe int GetNumReadHandles(ComponentDependencyManager* componentDependencyManager, in ComponentType componentType)
        {
            TypeIndex typeIndex = componentType.TypeIndex;

            byte* address = (byte*)componentDependencyManager;
            byte* typeArrayIndicesAddress = address + FIELD_OFFSET_TYPE_ARRAY_INDICES;
            byte* dependencyHandlesAddress = address + FIELD_OFFSET_DEPENDENCY_HANDLES;

            ushort* typeArrayIndices = *(ushort**)typeArrayIndicesAddress;
            DependencyHandle* dependencyHandles = *(DependencyHandle**)dependencyHandlesAddress;

            ushort arrayIndex = typeArrayIndices[typeIndex.Index];
            DependencyHandle dependencyHandle = dependencyHandles[arrayIndex];
            return dependencyHandle.NumReadFences;
        }
    }
}