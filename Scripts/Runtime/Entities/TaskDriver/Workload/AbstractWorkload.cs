using Anvil.CSharp.Collections;
using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal abstract class AbstractWorkload : AbstractAnvilBase
    {
        protected static readonly Type I_DRIVER_CANCELLABLE_DATA_STREAM = typeof(IDriverCancellableDataStream<>);
        protected static readonly Type I_DRIVER_DATA_STREAM = typeof(IDriverDataStream<>);
        protected static readonly Type I_DRIVER_CANCEL_RESULT_DATA_STREAM = typeof(IDriverCancelResultDataStream<>);
        protected static readonly Type I_SYSTEM_CANCELLABLE_DATA_STREAM = typeof(ISystemCancellableDataStream<>);
        protected static readonly Type I_SYSTEM_DATA_STREAM = typeof(ISystemDataStream<>);
        protected static readonly Type I_SYSTEM_CANCEL_RESULT_DATA_STREAM = typeof(ISystemCancelResultDataStream<>);

        private static readonly Type I_ENTITY_PROXY_INSTANCE_TYPE = typeof(IEntityProxyInstance);
        private static readonly Dictionary<Type, MethodInfo> TYPED_GENERIC_CREATION_METHODS = new Dictionary<Type, MethodInfo>();
        private static readonly MethodInfo PROTOTYPE_DATA_STREAM_METHOD = typeof(AbstractWorkload).GetMethod(nameof(CreateTypedDataStream), BindingFlags.Static | BindingFlags.NonPublic);


        public readonly List<AbstractDataStream> DataStreams;
        public readonly List<AbstractDataStream> CancellableDataStreams;
        public readonly List<AbstractDataStream> CancelResultDataStreams;
        public readonly CancelRequestDataStream CancelRequestDataStream;
        public readonly CancelCompleteDataStream CancelCompleteDataStream;
        public readonly AccessControlledValue<UnsafeParallelHashMap<EntityProxyInstanceID, bool>> CancelProgressLookup;

        public readonly List<AbstractDataStream> AllPublicDataStreams;


        protected Type TaskDriverType { get; }
        protected abstract HashSet<Type> ValidDataStreamTypes { get; }

        public AbstractTaskDriverSystem GoverningSystem { get; }
        
        public bool HasCancellableData { get; }
        
        public byte Context { get; }
        
        public World World { get; }

        protected AbstractWorkload(World world, Type taskDriverType, AbstractTaskDriverSystem governingSystem)
        {
            World = world;
            Context = GenerateContext();
            TaskDriverType = taskDriverType;
            GoverningSystem = governingSystem;

            AllPublicDataStreams = new List<AbstractDataStream>();

            DataStreams = new List<AbstractDataStream>();
            CancellableDataStreams = new List<AbstractDataStream>();
            CancelResultDataStreams = new List<AbstractDataStream>();
            CancelProgressLookup = new AccessControlledValue<UnsafeParallelHashMap<EntityProxyInstanceID, bool>>(new UnsafeParallelHashMap<EntityProxyInstanceID, bool>(ChunkUtil.MaxElementsPerChunk<EntityProxyInstanceID>(),
                                                                                                                                                                        Allocator.Persistent));
            CancelCompleteDataStream = new CancelCompleteDataStream(this);
            CancelRequestDataStream = new CancelRequestDataStream(CancelProgressLookup,
                                                                  CancelCompleteDataStream,
                                                                  this);

            CreateDataStreams();

            HasCancellableData = InitHasCancellableData();
        }

        protected abstract byte GenerateContext();

        protected virtual bool InitHasCancellableData()
        {
            return CancellableDataStreams.Count > 0;
        }

        protected override void DisposeSelf()
        {
            //TODO: #104 - Should this get baked into AccessControlledValue's Dispose method?
            CancelProgressLookup.Acquire(AccessType.Disposal);
            CancelProgressLookup.Dispose();

            CancelRequestDataStream.Dispose();
            CancelCompleteDataStream.Dispose();

            AllPublicDataStreams.DisposeAllAndTryClear();

            DataStreams.Clear();
            CancellableDataStreams.Clear();
            CancelResultDataStreams.Clear();

            base.DisposeSelf();
        }

        private void CreateDataStreams()
        {
            FieldInfo[] fields = TaskDriverType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (FieldInfo field in fields)
            {
                Type fieldType = field.FieldType;
                //Can't create if it's not a generic type...
                if (!fieldType.IsGenericType)
                {
                    continue;
                }

                Type genericTypeDefinition = fieldType.GetGenericTypeDefinition();

                //If it's not a valid type to create...
                if (!ValidDataStreamTypes.Contains(genericTypeDefinition))
                {
                    continue;
                }

                //If the field should be ignored and not auto-generated...
                IgnoreDataStreamAttribute ignoreDataStreamAttribute = field.GetCustomAttribute<IgnoreDataStreamAttribute>();
                if (ignoreDataStreamAttribute != null)
                {
                    continue;
                }

                Debug_CheckFieldIsReadOnly(field);
                Debug_CheckFieldTypeGenericTypeArguments(fieldType);

                Type instanceType = fieldType.GenericTypeArguments[0];

                Debug_CheckInstanceType(instanceType);

                InitDataStreamInstance(field, instanceType, genericTypeDefinition);
            }
        }

        protected AbstractDataStream CreateDataStreamInstance(Type instanceType, Type genericTypeDefinition)
        {
            MethodInfo createFunction = GetOrCreateMethod(instanceType);
            AbstractDataStream createdInstance = (AbstractDataStream)createFunction.Invoke(null,
                                                                                           new object[]
                                                                                           {
                                                                                               genericTypeDefinition
                                                                                           });
            return createdInstance;
        }

        protected void AssignDataStreamInstance(FieldInfo field, AbstractTaskDriver taskDriver, AbstractDataStream dataStream)
        {
            Debug_EnsureFieldNotSet(field, taskDriver);
            field.SetValue(taskDriver, dataStream);
        }

        private MethodInfo GetOrCreateMethod(Type instanceType)
        {
            if (!TYPED_GENERIC_CREATION_METHODS.TryGetValue(instanceType, out MethodInfo typedGenericMethod))
            {
                typedGenericMethod = PROTOTYPE_DATA_STREAM_METHOD.MakeGenericMethod(instanceType);
                TYPED_GENERIC_CREATION_METHODS.Add(instanceType, typedGenericMethod);
            }

            return typedGenericMethod;
        }

        private AbstractDataStream CreateTypedDataStream<TInstance>(Type genericTypeDefinition)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            AbstractDataStream dataStream = null;

            if (genericTypeDefinition == I_DRIVER_DATA_STREAM
             || genericTypeDefinition == I_SYSTEM_DATA_STREAM)
            {
                dataStream = new DataStream<TInstance>(CancelRequestDataStream, this);
                DataStreams.Add(dataStream);
            }
            else if (genericTypeDefinition == I_DRIVER_CANCELLABLE_DATA_STREAM
                  || genericTypeDefinition == I_SYSTEM_CANCELLABLE_DATA_STREAM)
            {
                dataStream = new CancellableDataStream<TInstance>(CancelRequestDataStream, this);
                CancellableDataStreams.Add(dataStream);
            }
            else if (genericTypeDefinition == I_DRIVER_CANCEL_RESULT_DATA_STREAM
                  || genericTypeDefinition == I_SYSTEM_CANCEL_RESULT_DATA_STREAM)
            {
                dataStream = new CancelResultDataStream<TInstance>(this);
                CancelResultDataStreams.Add(dataStream);
            }
            else
            {
                throw new InvalidOperationException($"Trying to create an instance of {nameof(AbstractDataStream)} but the generic type definition of {genericTypeDefinition} has no valid code path!");
            }

            AllPublicDataStreams.Add(dataStream);
            return dataStream;
        }

        protected abstract void InitDataStreamInstance(FieldInfo taskDriverField, Type instanceType, Type genericTypeDefinition);

        public override string ToString()
        {
            //TODO: Implement based on TaskDriver and what data we are. 
            return base.ToString();
        }

        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void Debug_CheckInstanceType(Type proxyInstanceType)
        {
            if (!I_ENTITY_PROXY_INSTANCE_TYPE.IsAssignableFrom(proxyInstanceType))
            {
                throw new InvalidOperationException($"Type {proxyInstanceType} does not implement {I_ENTITY_PROXY_INSTANCE_TYPE}!");
            }

            if (!UnsafeUtility.IsUnmanaged(proxyInstanceType))
            {
                throw new InvalidOperationException($"Type {proxyInstanceType} is not unmanaged!");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void Debug_CheckFieldIsReadOnly(FieldInfo fieldInfo)
        {
            if (!fieldInfo.IsInitOnly)
            {
                throw new InvalidOperationException($"Field with name {fieldInfo.Name} on {fieldInfo.ReflectedType} is not marked as \"readonly\", please ensure that it is.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void Debug_CheckFieldTypeGenericTypeArguments(Type fieldType)
        {
            if (fieldType.GenericTypeArguments.Length != 1)
            {
                throw new InvalidOperationException($"Type {fieldType} is to be used to create a {typeof(DataStream<>)} but {fieldType} doesn't have the expected 1 generic type!");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void Debug_EnsureFieldNotSet(FieldInfo fieldInfo, AbstractTaskDriver taskDriver)
        {
            if (fieldInfo.GetValue(taskDriver) != null)
            {
                throw new InvalidOperationException($"Field with name {fieldInfo.Name} on {fieldInfo.ReflectedType} is already set! Did you call manually create it?");
            }
        }
    }
}
