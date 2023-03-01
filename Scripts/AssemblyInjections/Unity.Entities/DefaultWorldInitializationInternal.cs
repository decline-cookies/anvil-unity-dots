using System;
using Unity.Entities;

public static class DefaultWorldInitializationInternal
{
    /// <inheritdoc cref="DefaultWorldInitialization.DefaultWorldInitialized"/>
    public static event Action<World> DefaultWorldInitialized
    {
        add => DefaultWorldInitialization.DefaultWorldInitialized += value;
        remove => DefaultWorldInitialization.DefaultWorldInitialized -= value;
    }

    /// <inheritdoc cref="DefaultWorldInitialization.DefaultWorldDestroyed"/>
    public static event Action DefaultWorldDestroyed
    {
        add => DefaultWorldInitialization.DefaultWorldDestroyed += value;
        remove => DefaultWorldInitialization.DefaultWorldDestroyed -= value;
    }
}