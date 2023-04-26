using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

// TODO: #228 - Create a data structure to more efficiently interact with these methods every frame

/// <summary>
/// A collection of extension methods to help calculate and modify the dependencies on <see cref="ComponentType"/>s
/// directly.
/// </summary>
[BurstCompatible]
public static class ComponentTypeDependencyExtension
{
    private static UnsafeList<int> s_WriteTypeList_ScratchPad;
    private static UnsafeList<int> s_ReadTypeList_ScratchPad;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Init()
    {
        if (s_WriteTypeList_ScratchPad.IsCreated)
        {
            s_WriteTypeList_ScratchPad.Dispose();
        }
        s_WriteTypeList_ScratchPad = new UnsafeList<int>(0, Allocator.Persistent);

        if (s_ReadTypeList_ScratchPad.IsCreated)
        {
            s_ReadTypeList_ScratchPad.Dispose();
        }
        s_ReadTypeList_ScratchPad = new UnsafeList<int>(0, Allocator.Persistent);
    }


    /// <summary>
    /// Get the dependency of an individual component type.
    /// Useful for scheduling jobs that depend on components out of band with a system's
    /// <see cref="ComponentSystemBase.OnUpdate"/>.
    /// </summary>
    /// <param name="manager"> The <see cref="World"/>'s <see cref="EntityManager"/>.</param>
    /// <param name="componentTypes">The component type to get the dependency of.</param>
    /// <returns>The dependency for the component types</returns>
    public static unsafe JobHandle GetDependency(this EntityManager manager, ComponentType componentType)
    {
        return GetDependency(manager.GetCheckedEntityDataAccess()->DependencyManager, componentType);
    }

    /// <summary>
    /// Get the combined dependency of a collection of component types.
    /// Useful for scheduling jobs that depend on components out of band with a system's
    /// <see cref="ComponentSystemBase.OnUpdate"/>.
    /// </summary>
    /// <param name="manager"> The <see cref="World"/>'s <see cref="EntityManager"/>.</param>
    /// <param name="componentTypes">The component types to calculate the dependency of.</param>
    /// <returns>The combined dependency for the component types</returns>
    [NotBurstCompatible]
    public static unsafe JobHandle GetDependency(this EntityManager manager, params ComponentType[] componentTypes)
    {
        return GetDependency(manager.GetCheckedEntityDataAccess()->DependencyManager, componentTypes);
    }

    /// <summary>
    /// Get the combined dependency of a collection of component types.
    /// Useful for scheduling jobs that depend on components out of band with a system's
    /// <see cref="ComponentSystemBase.OnUpdate"/>.
    /// </summary>
    /// <typeparam name="T">The collection type.</typeparam>
    /// <param name="manager"> The <see cref="World"/>'s <see cref="EntityManager"/>.</param>
    /// <param name="componentTypes">The component types to calculate the dependency of.</param>
    /// <returns>The combined dependency for the component types</returns>
    [NotBurstCompatible]
    public static unsafe JobHandle GetDependency<T>(this EntityManager manager, T componentTypes)
        where T : class, IEnumerable<ComponentType>
    {
        return GetDependency(manager.GetCheckedEntityDataAccess()->DependencyManager, componentTypes);
    }

    /// <summary>
    /// Get the combined dependency of a collection of component types.
    /// Useful for scheduling jobs that depend on components out of band with a system's
    /// <see cref="ComponentSystemBase.OnUpdate"/>.
    /// </summary>
    /// <param name="manager"> The <see cref="World"/>'s <see cref="EntityManager"/>.</param>
    /// <param name="componentTypes">The component types to calculate the dependency of.</param>
    /// <returns>The combined dependency for the component types</returns>
    public static unsafe JobHandle GetDependency(this EntityManager manager, ref NativeArray<ComponentType> componentTypes)
    {
        return GetDependency(manager.GetCheckedEntityDataAccess()->DependencyManager, ref componentTypes);
    }

    /// <summary>
    /// Get the dependency of an individual component type.
    /// Useful for scheduling jobs out of band with a system's <see cref="ComponentSystemBase.OnUpdate"/>.
    /// </summary>
    /// <param name="componentType">The component type to get the dependency of.</param>
    /// <param name="manager"> The <see cref="World"/>'s <see cref="EntityManager"/>.</param>
    /// <returns>The dependency for the component type</returns>
    public static unsafe JobHandle GetDependency(this ComponentType componentType, EntityManager manager)
    {
        return GetDependency(manager.GetCheckedEntityDataAccess()->DependencyManager, componentType);
    }

    /// <summary>
    /// Get the combined dependency of a collection of component types.
    /// Useful for scheduling jobs out of band with a system's <see cref="ComponentSystemBase.OnUpdate"/>.
    /// </summary>
    /// <typeparam name="T">The collection type.</typeparam>
    /// <param name="componentTypes">The component types to calculate the dependency of.</param>
    /// <param name="manager"> The <see cref="World"/>'s <see cref="EntityManager"/>.</param>
    /// <returns>The combined dependency for the component types</returns>
    [NotBurstCompatible]
    public static unsafe JobHandle GetDependency<T>(this T componentTypes, EntityManager manager)
        where T : class, IEnumerable<ComponentType>
    {
        return GetDependency(manager.GetCheckedEntityDataAccess()->DependencyManager, componentTypes);
    }

    /// <summary>
    /// Get the combined dependency of a collection of component types.
    /// Useful for scheduling jobs out of band with a system's <see cref="ComponentSystemBase.OnUpdate"/>.
    /// </summary>
    /// <param name="componentTypes">The component types to calculate the dependency of.</param>
    /// <param name="manager"> The <see cref="World"/>'s <see cref="EntityManager"/>.</param>
    /// <returns>The combined dependency for the component types</returns>
    [NotBurstCompatible]
    public static unsafe JobHandle GetDependency(this ref NativeArray<ComponentType> componentTypes, EntityManager manager)
    {
        return GetDependency(manager.GetCheckedEntityDataAccess()->DependencyManager, ref componentTypes);
    }

    /// <summary>
    /// Sets the dependency of an individual component type.
    /// Useful for scheduling jobs that depend on components out of band with a system's
    /// <see cref="ComponentSystemBase.OnUpdate"/>.
    /// </summary>
    /// <param name="manager"> The <see cref="World"/>'s <see cref="EntityManager"/>.</param>
    /// <param name="dependency">The handle that represents when the consuming job is complete.</param>
    /// <param name="componentTypes">The component type to set the dependency of.</param>
    public static unsafe void AddDependency(this EntityManager manager, JobHandle dependency, ComponentType componentType)
    {
        AddDependency(manager.GetCheckedEntityDataAccess()->DependencyManager, dependency, componentType);
    }

    /// <summary>
    /// Sets the combined dependency of a collection of component types.
    /// Useful for scheduling jobs that depend on components out of band with a system's
    /// <see cref="ComponentSystemBase.OnUpdate"/>.
    /// </summary>
    /// <param name="manager"> The <see cref="World"/>'s <see cref="EntityManager"/>.</param>
    /// <param name="dependency">The handle that represents when the consuming job is complete.</param>
    /// <param name="componentTypes">The component types to set the dependency of.</param>
    [NotBurstCompatible]
    public static unsafe void AddDependency(this EntityManager manager, JobHandle dependency, params ComponentType[] componentTypes)
    {
        AddDependency(manager.GetCheckedEntityDataAccess()->DependencyManager, dependency, componentTypes);
    }

    /// <summary>
    /// Sets the combined dependency of a collection of component types.
    /// Useful for scheduling jobs that depend on components out of band with a system's
    /// <see cref="ComponentSystemBase.OnUpdate"/>.
    /// </summary>
    /// <typeparam name="T">The collection type.</typeparam>
    /// <param name="manager"> The <see cref="World"/>'s <see cref="EntityManager"/>.</param>
    /// <param name="dependency">The handle that represents when the consuming job is complete.</param>
    /// <param name="componentTypes">The component types to set the dependency of.</param>
    [NotBurstCompatible]
    public static unsafe void AddDependency<T>(this EntityManager manager, JobHandle dependency, T componentTypes)
        where T : class, IEnumerable<ComponentType>
    {
        AddDependency(manager.GetCheckedEntityDataAccess()->DependencyManager, dependency, componentTypes);
    }

    /// <summary>
    /// Sets the combined dependency of a collection of component types.
    /// Useful for scheduling jobs that depend on components out of band with a system's
    /// <see cref="ComponentSystemBase.OnUpdate"/>.
    /// </summary>
    /// <param name="manager"> The <see cref="World"/>'s <see cref="EntityManager"/>.</param>
    /// <param name="dependency">The handle that represents when the consuming job is complete.</param>
    /// <param name="componentTypes">The component types to set the dependency of.</param>
    public static unsafe void AddDependency(this EntityManager manager, JobHandle dependency, ref NativeArray<ComponentType> componentTypes)
    {
        AddDependency(manager.GetCheckedEntityDataAccess()->DependencyManager, dependency, ref componentTypes);
    }

    /// <summary>
    /// Sets the dependency of an individual component type.
    /// Useful for scheduling jobs out of band with a system's <see cref="ComponentSystemBase.OnUpdate"/>.
    /// </summary>
    /// <param name="componentType">The component type to set the dependency of.</param>
    /// <param name="dependency">The handle that represents when the consuming job is complete.</param>
    /// <param name="manager"> The <see cref="World"/>'s <see cref="EntityManager"/>.</param>
    public static unsafe void AddDependency(this ComponentType componentType, JobHandle dependency, EntityManager manager)
    {
        AddDependency(manager.GetCheckedEntityDataAccess()->DependencyManager, dependency, componentType);
    }

    /// <summary>
    /// Set the combined dependency of a collection of component types.
    /// Useful for scheduling jobs out of band with a system's <see cref="ComponentSystemBase.OnUpdate"/>.
    /// </summary>
    /// <typeparam name="T">The collection type.</typeparam>
    /// <param name="componentTypes">The component types to set the dependency of.</param>
    /// <param name="dependency">The handle that represents when the consuming job is complete.</param>
    /// <param name="manager"> The <see cref="World"/>'s <see cref="EntityManager"/>.</param>
    [NotBurstCompatible]
    public static unsafe void AddDependency<T>(this T componentTypes, JobHandle dependency, EntityManager manager)
        where T : class, IEnumerable<ComponentType>
    {
        AddDependency(manager.GetCheckedEntityDataAccess()->DependencyManager, dependency, componentTypes);
    }

    /// <summary>
    /// Set the combined dependency of a collection of component types.
    /// Useful for scheduling jobs out of band with a system's <see cref="ComponentSystemBase.OnUpdate"/>.
    /// </summary>
    /// <param name="componentTypes">The component types to set the dependency of.</param>
    /// <param name="dependency">The handle that represents when the consuming job is complete.</param>
    /// <param name="manager"> The <see cref="World"/>'s <see cref="EntityManager"/>.</param>
    [NotBurstCompatible]
    public static unsafe void AddDependency(this ref NativeArray<ComponentType> componentTypes, JobHandle dependency, EntityManager manager)
    {
        AddDependency(manager.GetCheckedEntityDataAccess()->DependencyManager, dependency, ref componentTypes);
    }

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

    [NotBurstCompatible]
    private static unsafe JobHandle GetDependency<T>(ComponentDependencyManager* dependencyManager, T componentTypes)
        where T : class, IEnumerable<ComponentType>
    {
        BuildTypeLists(componentTypes);
        return dependencyManager
            ->GetDependency(s_ReadTypeList_ScratchPad.Ptr, s_ReadTypeList_ScratchPad.Length, s_WriteTypeList_ScratchPad.Ptr, s_WriteTypeList_ScratchPad.Length);
    }

    private static unsafe JobHandle GetDependency(ComponentDependencyManager* dependencyManager, ref NativeArray<ComponentType> componentTypes)
    {
        BuildTypeLists(ref componentTypes);
        return dependencyManager
            ->GetDependency(s_ReadTypeList_ScratchPad.Ptr, s_ReadTypeList_ScratchPad.Length, s_WriteTypeList_ScratchPad.Ptr, s_WriteTypeList_ScratchPad.Length);
    }

    private static unsafe void AddDependency(ComponentDependencyManager* dependencyManager, JobHandle dependency, ComponentType componentType)
    {
        // Micro-optimization
        // Since we're ony dealing with one component we can skip building a list.
        //  1. Get the pointer to the one type index.
        //  2. Use the pointer for both the reader and writer parameters
        //  3. Calculate the reader/writer count based on the component type's access mode.
        int* typeIndexPtr = (int*)UnsafeUtility.AddressOf(ref componentType.TypeIndex);
        int writerCount = componentType.AccessModeType == ComponentType.AccessMode.ReadWrite ? 1 : 0;
        int readerCount = 1 - writerCount;

        dependencyManager
            ->AddDependency(typeIndexPtr, readerCount, typeIndexPtr, writerCount, dependency);
    }

    [NotBurstCompatible]
    private static unsafe void AddDependency<T>(ComponentDependencyManager* dependencyManager, JobHandle dependency, T componentTypes)
        where T : class, IEnumerable<ComponentType>
    {
        BuildTypeLists(componentTypes);
        dependencyManager
            ->AddDependency(s_ReadTypeList_ScratchPad.Ptr, s_ReadTypeList_ScratchPad.Length, s_WriteTypeList_ScratchPad.Ptr, s_WriteTypeList_ScratchPad.Length, dependency);
    }

    private static unsafe void AddDependency(ComponentDependencyManager* dependencyManager, JobHandle dependency, ref NativeArray<ComponentType> componentTypes)
    {
        BuildTypeLists(ref componentTypes);
        dependencyManager
            ->AddDependency(s_ReadTypeList_ScratchPad.Ptr, s_ReadTypeList_ScratchPad.Length, s_WriteTypeList_ScratchPad.Ptr, s_WriteTypeList_ScratchPad.Length, dependency);
    }

    [NotBurstCompatible]
    private static void BuildTypeLists<T>(T componentTypes)
        where T : class, IEnumerable<ComponentType>
    {
        s_WriteTypeList_ScratchPad.Clear();
        s_ReadTypeList_ScratchPad.Clear();

        foreach (ComponentType componentType in componentTypes)
        {
            CalculateReaderWriterDependency.Add(componentType, ref s_ReadTypeList_ScratchPad, ref s_WriteTypeList_ScratchPad);
        }
    }

    private static void BuildTypeLists(ref NativeArray<ComponentType> componentTypes)
    {
        s_WriteTypeList_ScratchPad.Clear();
        s_ReadTypeList_ScratchPad.Clear();

        foreach (ComponentType componentType in componentTypes)
        {
            CalculateReaderWriterDependency.Add(componentType, ref s_ReadTypeList_ScratchPad, ref s_WriteTypeList_ScratchPad);
        }
    }
}