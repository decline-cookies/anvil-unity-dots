using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Entities;
using UnityEngine;

namespace Anvil.Unity.DOTS.Entities
{
    internal static class EntitySpawnSystemReflectionHelper
    {
        public static readonly Dictionary<Type, IEntitySpawnDefinition> SPAWN_DEFINITION_TYPES = new Dictionary<Type, IEntitySpawnDefinition>();

        private static readonly Type I_ENTITY_SPAWN_DEFINITION_TYPE = typeof(IEntitySpawnDefinition);
        //TODO: #86 - Remove once we don't have to switch with BURST
        public static readonly Dictionary<Type, bool> SHOULD_DISABLE_BURST_LOOKUP = new Dictionary<Type, bool>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            SPAWN_DEFINITION_TYPES.Clear();
            SHOULD_DISABLE_BURST_LOOKUP.Clear();
            //We'll reflect through the whole app to find all the possible IEntitySpawnDefinitions that exist. 
            //We do this once here so we don't have to reflect for each World that exists.
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                ParseAssembly(assembly);
            }
        }

        private static void ParseAssembly(Assembly assembly)
        {
            if (assembly.IsDynamic)
            {
                return;
            }
            foreach (Type type in assembly.GetTypes())
            {
                ParseType(type);
            }
        }

        private static void ParseType(Type type)
        {
            if (!type.IsValueType || !I_ENTITY_SPAWN_DEFINITION_TYPE.IsAssignableFrom(type))
            {
                return;
            }

            SPAWN_DEFINITION_TYPES.Add(type, (IEntitySpawnDefinition)Activator.CreateInstance(type));

            if (SHOULD_DISABLE_BURST_LOOKUP.ContainsKey(type))
            {
                return;
            }
            SHOULD_DISABLE_BURST_LOOKUP.Add(type, ShouldDisableBurst(type));
        }

        //TODO: #86 - Remove once we don't have to switch with BURST
        private static bool ShouldDisableBurst(Type type)
        {
            //We've already processed this type and it exists in the lookup, we can just return
            if (SHOULD_DISABLE_BURST_LOOKUP.TryGetValue(type, out bool shouldDisableBurst))
            {
                return shouldDisableBurst;
            }

            //If any of our components require disabling burst we can early exit.
            if (ShouldRequiredComponentsDisableBurst(type))
            {
                return true;
            }

            //Otherwise crawl our fields and properties to see if there are any proxy definitions that would require us
            //to also disable burst
            Type[] definitionTypes = type
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Select(fieldInfo => fieldInfo.FieldType)
                .Union(
                    type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        .Select(propertyInfo => propertyInfo.PropertyType))
                .ToArray();

            foreach (Type definitionType in definitionTypes)
            {
                if (!I_ENTITY_SPAWN_DEFINITION_TYPE.IsAssignableFrom(definitionType))
                {
                    continue;
                }

                //We have at least one field that needs us to disable burst, early out.
                if (ShouldRequiredComponentsDisableBurst(definitionType))
                {
                    return true;
                }

                //Let's dive in deeper
                if (ShouldDisableBurst(definitionType))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ShouldRequiredComponentsDisableBurst(Type type)
        {
            ComponentType[] requiredComponents = SPAWN_DEFINITION_TYPES[type].RequiredComponents;
            return requiredComponents.Any(componentType => componentType.IsSharedComponent);
        }
    }
}
