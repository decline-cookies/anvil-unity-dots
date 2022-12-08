using Anvil.CSharp.Data;
using Anvil.CSharp.Logging;
using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal abstract partial class AbstractTaskDriverSystem : AbstractAnvilSystemBase,
                                                               ITaskSetOwner
    {
        private readonly IDProvider m_TaskDriverIDProvider;
        private readonly List<AbstractTaskDriver> m_TaskDrivers;
        private readonly Type m_Type;
        

        private bool m_IsInitialized;

        public TaskSet TaskSet { get; private set; }
        public uint ID { get; }

        public AbstractTaskDriverSystem TaskDriverSystem { get => this; }

        public Type TaskDriverType { get; }
        
        public new World World { get; private set; }
        
        protected AbstractTaskDriverSystem()
        {
            m_Type = GetType();
            TaskDriverType = m_Type.GenericTypeArguments[0];

            m_TaskDriverIDProvider = new IDProvider();
            m_TaskDrivers = new List<AbstractTaskDriver>();

            ID = m_TaskDriverIDProvider.GetNextID();
        }

        public void Init(World world)
        {
            World = world;
            //Because we don't know which TaskDriver will create this system, it can be called
            //multiple times. We only need a world reference once and we don't get that until 
            //OnCreate for the system.
            if (m_IsInitialized)
            {
                return;
            }

            m_IsInitialized = true;

            TaskSet = TaskSetConstructionUtil.CreateTaskSetForTaskSystem(this);
        }

        protected override void OnDestroy()
        {
            m_TaskDriverIDProvider.Dispose();
            //We don't own the TaskDrivers registered here, so we won't dispose them
            m_TaskDrivers.Clear();

            TaskSet.Dispose();

            base.OnDestroy();
        }

        public override string ToString()
        {
            return m_Type.GetReadableName();
        }

        public uint RegisterTaskDriver(AbstractTaskDriver taskDriver)
        {
            uint taskDriverID = m_TaskDriverIDProvider.GetNextID();
            m_TaskDrivers.Add(taskDriver);
            return taskDriverID;
        }

        protected override void OnUpdate()
        {
            //TODO: Implement
            // Dependency = CommonTaskSet.Update(Dependency);
        }
    }
}
