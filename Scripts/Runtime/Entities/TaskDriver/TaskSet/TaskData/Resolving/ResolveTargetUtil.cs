using Anvil.CSharp.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using UnityEngine;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    [BurstCompatible]
    internal static class ResolveTargetUtil
    {
        // ReSharper disable once ClassNeverInstantiated.Local
        private class ResolveTargetSharedStaticContext { }

        [BurstCompatible]
        // ReSharper disable once ClassNeverInstantiated.Local
        private class ResolveTargetID<TResolveTargetType>
            where TResolveTargetType : unmanaged, IEntityProxyInstance
        {
            // ReSharper disable once StaticMemberInGenericType
            public static readonly SharedStatic<uint> ID
                = SharedStatic<uint>.GetOrCreate<ResolveTargetSharedStaticContext, TResolveTargetType>();
        }

        /// <summary>
        /// The ID's for each ResolveTarget are not deterministic, it's a first come first serve basis.
        /// As code changes through development, an older save file might have a different ID for the same type.
        /// This mapping can be used to stitch them properly together.
        /// </summary>
        /// TODO: #83 - Implement serialization
        internal static readonly Dictionary<Type, uint> SERIALIZATION_MAPPING = new Dictionary<Type, uint>();

        private static IDProvider s_IDProvider;


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Init()
        {
            s_IDProvider?.Dispose();
            s_IDProvider = new IDProvider();
            SERIALIZATION_MAPPING.Clear();
        }

        public static uint RegisterResolveTarget<TResolveTargetType>()
            where TResolveTargetType : unmanaged, IEntityProxyInstance
        {
            uint id = ResolveTargetID<TResolveTargetType>.ID.Data;
            if (id != IDProvider.UNSET_ID)
            {
                return id;
            }

            id = s_IDProvider.GetNextID();
            ResolveTargetID<TResolveTargetType>.ID.Data = id;
            SERIALIZATION_MAPPING.Add(typeof(TResolveTargetType), id);

            return id;
        }

        [BurstCompatible]
        public static uint GetResolveTargetID<TResolveTargetType>()
            where TResolveTargetType : unmanaged, IEntityProxyInstance
        {
            uint id = ResolveTargetID<TResolveTargetType>.ID.Data;
            Debug_EnsureIsRegistered<TResolveTargetType>(id);

            return id;
        }

        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void Debug_EnsureIsRegistered<TResolveTargetType>(uint id)
            where TResolveTargetType : unmanaged, IEntityProxyInstance
        {
            if (id == IDProvider.UNSET_ID)
            {
                throw new InvalidOperationException($"Trying to get id for {default(TResolveTargetType)} but it was never registered! Did you call {nameof(IResolvableJobConfigRequirements.RequireResolveTarget)} for your job?");
            }
        }
    }
}