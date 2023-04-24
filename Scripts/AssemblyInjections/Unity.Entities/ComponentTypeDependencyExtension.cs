using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

/// <summary>
/// A collection of extension methods to help calculate the scheduling dependencies on <see cref="ComponentType"/>s.
/// </summary>
[BurstCompatible]
public static class ComponentTypeDependencyExtension
{
    private static UnsafeList<int> s_WriteTypeList_ScratchPad;
    private static UnsafeList<int> s_ReadTypeList_ScratchPad;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Init()
    {
        s_WriteTypeList_ScratchPad.Dispose();
        s_WriteTypeList_ScratchPad = new UnsafeList<int>(0, Allocator.Persistent);

        s_ReadTypeList_ScratchPad.Dispose();
        s_ReadTypeList_ScratchPad = new UnsafeList<int>(0, Allocator.Persistent);
    }

    /// <summary>
    /// Get the dependency of an individual component type.
    /// Useful for scheduling jobs that depend on components out of band with a system's
    /// <see cref="ComponentSystemBase.OnUpdate"/>.
    /// </summary>
    /// <param name="manager"> The <see cref="World"/>'s <see cref="EntityManager"/>.</param>
    /// <param name="component">The component type to get the dependency of.</param>
    /// <returns>The dependency for the component type</returns>
    public static unsafe JobHandle GetDependency(this EntityManager manager, ComponentType componentType)
    {
        return GetDependency(manager.GetUncheckedEntityDataAccess()->DependencyManager, componentType);
    }

    /// <summary>
    /// Get the combined dependency of a collection of component types.
    /// Useful for scheduling jobs that depend on components out of band with a system's
    /// <see cref="ComponentSystemBase.OnUpdate"/>.
    /// </summary>
    /// <param name="manager"> The <see cref="World"/>'s <see cref="EntityManager"/>.</param>
    /// <param name="component">The component types to calculate the dependency of.</param>
    /// <returns>The combined dependency for the component types</returns>
    [NotBurstCompatible]
    public static unsafe JobHandle GetDependency(this EntityManager manager, params ComponentType[] componentTypes)
    {
        return GetDependency(manager.GetUncheckedEntityDataAccess()->DependencyManager, componentTypes);
    }

    /// <summary>
    /// Get the combined dependency of a collection of component types.
    /// Useful for scheduling jobs that depend on components out of band with a system's
    /// <see cref="ComponentSystemBase.OnUpdate"/>.
    /// </summary>
    /// <param name="manager"> The <see cref="World"/>'s <see cref="EntityManager"/>.</param>
    /// <param name="component">The component types to calculate the dependency of.</param>
    /// <returns>The combined dependency for the component types</returns>
    public static unsafe JobHandle GetDependency(this EntityManager manager, NativeArray<ComponentType> componentTypes)
    {
        return GetDependency(manager.GetUncheckedEntityDataAccess()->DependencyManager, componentTypes);
    }

    /// <summary>
    /// Get the dependency of an individual component type.
    /// Useful for scheduling jobs out of band with a system's <see cref="ComponentSystemBase.OnUpdate"/>.
    /// </summary>
    /// <param name="system">
    /// The system running the out of band job. If not in a system use the <see cref="EntityManager"/> extension instead.
    /// </param>
    /// <param name="component">The component type to get the dependency of.</param>
    /// <returns>The dependency for the component type</returns>
    public static unsafe JobHandle GetDependency(this ComponentSystemBase system, ComponentType componentType)
    {
        return GetDependency(system.CheckedState()->m_DependencyManager, componentType);
    }

    /// <summary>
    /// Get the combined dependency of a collection of component types.
    /// Useful for scheduling jobs out of band with a system's <see cref="ComponentSystemBase.OnUpdate"/>.
    /// </summary>
    /// <param name="system">
    /// The system running the out of band job. If not in a system use the <see cref="EntityManager"/> extension instead.
    /// </param>
    /// <param name="component">The component types to calculate the dependency of.</param>
    /// <returns>The combined dependency for the component types</returns>
    [NotBurstCompatible]
    public static unsafe JobHandle GetDependency(this ComponentSystemBase system, params ComponentType[] componentTypes)
    {
        return GetDependency(system.CheckedState()->m_DependencyManager, componentTypes);
    }

    /// <summary>
    /// Get the combined dependency of a collection of component types.
    /// Useful for scheduling jobs out of band with a system's <see cref="ComponentSystemBase.OnUpdate"/>.
    /// </summary>
    /// <param name="system">
    /// The system running the out of band job. If not in a system use the <see cref="EntityManager"/> extension instead.
    /// </param>
    /// <param name="component">The component types to calculate the dependency of.</param>
    /// <returns>The combined dependency for the component types</returns>
    public static unsafe JobHandle GetDependency(this ComponentSystemBase system, NativeArray<ComponentType> componentTypes)
    {
        return GetDependency(system.CheckedState()->m_DependencyManager, componentTypes);
    }

    /// <summary>
    /// Get the dependency of an individual component type.
    /// Useful for scheduling jobs out of band with a system's <see cref="ComponentSystemBase.OnUpdate"/>.
    /// </summary>
    /// <param name="system">
    /// The system running the out of band job. If not in a system use the <see cref="EntityManager"/> extension instead.
    /// </param>
    /// <param name="componentType">The component type to get the dependency of.</param>
    /// <returns>The dependency for the component type</returns>
    private static unsafe JobHandle GetDependency(ComponentDependencyManager* dependencyManager, ComponentType componentType)
    {
        Debug.Assert(componentType.AccessModeType is ComponentType.AccessMode.ReadOnly or ComponentType.AccessMode.ReadWrite);

        // Micro-optimization
        // Since we're ony dealing with one component we can skip building a list.
        //  1. Get the pointer to the one type index.
        //  2. Use the pointer for both the reader and writer parameters
        //  3. Calculate the reader/writer count based on the component type's access mode.
        int* typeIndexPtr = (int*)UnsafeUtility.AddressOf(ref componentType.TypeIndex);
        int writerCount = componentType.AccessModeType == ComponentType.AccessMode.ReadWrite ? 1 : 0;
        int readerCount = 1 - writerCount;

        return dependencyManager
            ->GetDependency(typeIndexPtr, readerCount, typeIndexPtr, writerCount);
    }

    /// <summary>
    /// Get the combined dependency of a collection of component types.
    /// Useful for scheduling jobs out of band with a system's <see cref="ComponentSystemBase.OnUpdate"/>.
    /// </summary>
    /// <param name="system">
    /// The system running the out of band job. If not in a system use the <see cref="EntityManager"/> extension instead.
    /// </param>
    /// <param name="component">The component types to calculate the dependency of.</param>
    /// <returns>The combined dependency for the component types</returns>
    [NotBurstCompatible]
    private static unsafe JobHandle GetDependency(ComponentDependencyManager* dependencyManager, ComponentType[] componentTypes)
    {
        s_WriteTypeList_ScratchPad.Clear();
        s_ReadTypeList_ScratchPad.Clear();

        foreach (ComponentType componentType in componentTypes)
        {
            CalculateReaderWriterDependency.Add(componentType, ref s_ReadTypeList_ScratchPad, ref s_WriteTypeList_ScratchPad);
        }

        return dependencyManager
            ->GetDependency(s_ReadTypeList_ScratchPad.Ptr, s_ReadTypeList_ScratchPad.Length, s_WriteTypeList_ScratchPad.Ptr, s_WriteTypeList_ScratchPad.Length);
    }

    /// <summary>
    /// Get the combined dependency of a collection of component types.
    /// Useful for scheduling jobs out of band with a system's <see cref="ComponentSystemBase.OnUpdate"/>.
    /// </summary>
    /// <param name="system">
    /// The system running the out of band job. If not in a system use the <see cref="EntityManager"/> extension instead.
    /// </param>
    /// <param name="component">The component types to calculate the dependency of.</param>
    /// <returns>The combined dependency for the component types</returns>
    private static unsafe JobHandle GetDependency(ComponentDependencyManager* dependencyManager, NativeArray<ComponentType> componentTypes)
    {
        s_WriteTypeList_ScratchPad.Clear();
        s_ReadTypeList_ScratchPad.Clear();

        foreach (ComponentType componentType in componentTypes)
        {
            CalculateReaderWriterDependency.Add(componentType, ref s_ReadTypeList_ScratchPad, ref s_WriteTypeList_ScratchPad);
        }

        return dependencyManager
            ->GetDependency(s_ReadTypeList_ScratchPad.Ptr, s_ReadTypeList_ScratchPad.Length, s_WriteTypeList_ScratchPad.Ptr, s_WriteTypeList_ScratchPad.Length);
    }
}