using System;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class DataStreamNode : AbstractNode
    {
        private readonly Dictionary<Type, byte> m_ResolveTargetLookup;
        private readonly NodeLookup m_Lookup;

        public AbstractEntityProxyDataStream DataStream { get; }
        public AbstractTaskStream TaskStream { get; }

        public DataStreamNode(NodeLookup lookup,
                              AbstractEntityProxyDataStream dataStream,
                              TaskFlowGraph taskFlowGraph,
                              AbstractTaskSystem taskSystem,
                              AbstractTaskDriver taskDriver,
                              AbstractTaskStream taskStream) : base(taskFlowGraph, taskSystem, taskDriver)
        {
            m_Lookup = lookup;
            DataStream = dataStream;
            TaskStream = taskStream;

            m_ResolveTargetLookup = new Dictionary<Type, byte>();
        }

        protected override void DisposeSelf()
        {
            DataStream.Dispose();
            m_ResolveTargetLookup.Clear();

            base.DisposeSelf();
        }

        public override string ToString()
        {
            return $"{DataStream} as part of {TaskStream} located in {TaskDebugUtil.GetLocationName(TaskSystem, TaskDriver)}";
        }

        public void RegisterAsResolveTarget(ResolveTargetForAttribute resolveTargetAttribute)
        {
            byte value = (byte)resolveTargetAttribute.ResolveTarget;

            m_ResolveTargetLookup.Add(resolveTargetAttribute.ResolveTargetEnumType, value);
        }

        public bool IsResolveTarget<TResolveTarget>(TResolveTarget resolveTarget)
            where TResolveTarget : Enum
        {
            Type type = typeof(TResolveTarget);
            if (!m_ResolveTargetLookup.TryGetValue(type, out byte value))
            {
                return false;
            }
            
            ResolveTargetUtil.Debug_EnsureEnumValidity(resolveTarget);
            byte storedResolveTarget = UnsafeUtility.As<TResolveTarget, byte>(ref resolveTarget);
            return value == storedResolveTarget;
        }
    }
}
