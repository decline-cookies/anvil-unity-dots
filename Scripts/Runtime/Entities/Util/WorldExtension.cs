using Anvil.CSharp.Logging;
using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// A collection of extension methods for <see cref="World"/>.
    /// </summary>
    /// <remarks>
    /// Note: Most utilities are in <see cref="WorldUtil"/>. These methods are extension for better discoverability
    ///       of replacement or supplemental behaviour to what <see cref="World"/> already offers.
    /// </remarks>
    public static class WorldExtension
    {
        /// <summary>
        /// Creates (if required) the provided system types in the world sequentially and logs any errors during creation.
        /// Creating systems sequentially allows each system to get through <see cref="ComponentSystemBase.OnCreate"/>
        /// before the next system in the list is created. This allows systems to safely call
        /// <see cref="World.GetOrCreateSystem"/> from their own <see cref="ComponentSystemBase.OnCreate"/> and assume
        /// the returned instance has gone through its <see cref="ComponentSystemBase.OnCreate"/>
        ///
        /// In contrast, <see cref="World.GetOrCreateSystemsAndLogException"/> instantiates all system
        /// instances before looping through and calling <see cref="ComponentSystemBase.OnCreate"/> on each of them.
        /// This means that <see cref="World.GetOrCreateSystem"/> will return instances that have not had
        /// <see cref="ComponentSystemBase.OnCreate"/> called on them.
        /// </summary>
        /// <param name="world">The world to create the systems on.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the type does not derive from a valid type.
        /// (<see cref="ComponentSystemBase"/> or <see cref="ISystem"/>)
        /// </exception>
        public static void GetOrCreateSystemsSequentiallyAndLogExceptions(this World world, IEnumerable<Type> systemList)
        {
            foreach (Type systemType in systemList)
            {
                try
                {
                    if (typeof(ComponentSystemBase).IsAssignableFrom(systemType))
                        world.GetOrCreateSystem(systemType);
                    else if (typeof(ISystem).IsAssignableFrom(systemType))
                        world.GetOrCreateUnmanagedSystem(systemType);
                    else
                        throw new InvalidOperationException($"Invalid system type. Type:{systemType.GetReadableName()}");
                }
                catch (Exception ex)
                {
                    Log.GetStaticLogger(typeof(WorldUtil))
                        .Error($"Error creating system: {systemType.GetReadableName()} Error: (see next)");
                    Debug.LogException(ex);
                }
            }
        }
    }
}