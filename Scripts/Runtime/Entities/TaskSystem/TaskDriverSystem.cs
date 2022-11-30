using Anvil.CSharp.Collections;
using System;
using System.Collections.Generic;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class TaskDriverSystem : AbstractAnvilSystemBase
    {
        private readonly Dictionary<Type, CoreTaskDriverWork> m_CoreTaskDriverWorkLookup;

        public TaskDriverSystem()
        {
            m_CoreTaskDriverWorkLookup = new Dictionary<Type, CoreTaskDriverWork>();
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            Enabled = false;
        }

        protected override void OnDestroy()
        {
            m_CoreTaskDriverWorkLookup.DisposeAllValuesAndClear();
            base.OnDestroy();
        }

        public CoreTaskDriverWork GetOrCreateCoreTaskDriverWork(AbstractTaskDriver callingTaskDriver)
        {
            Type taskDriverType = callingTaskDriver.GetType();
            if (!m_CoreTaskDriverWorkLookup.TryGetValue(taskDriverType, out CoreTaskDriverWork coreTaskDriverWork))
            {
                coreTaskDriverWork = new CoreTaskDriverWork(taskDriverType);
                m_CoreTaskDriverWorkLookup.Add(taskDriverType, coreTaskDriverWork);
            }

            return coreTaskDriverWork;
        }

        protected override void OnUpdate()
        {
            //Just in case this gets enabled somehow
            Enabled = false;
        }
    }
}
