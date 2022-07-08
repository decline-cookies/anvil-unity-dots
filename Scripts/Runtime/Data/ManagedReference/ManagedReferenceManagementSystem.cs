using System;
using Anvil.Unity.DOTS.Entities;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Data
{
    public partial class ManagedReferenceManagementSystem : AbstractAnvilSystemBase
    {
        public ManagedReferenceManagementSystem()
        {

        }

        protected override void OnCreate()
        {
            base.OnCreate();

            Enabled = false;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            throw new NotSupportedException();
        }

        public void AddManagedRef<T>(T instance, bool allowOverwrite = false) where T : class, IComponentReferencable
        {
            if(!allowOverwrite && HasSingleton<ManagedReferenceComponent<T>>())
            {
                throw new InvalidOperationException("Managed ref already exists in environment. Use allowOverwrite to overwrite.");
            }

            SetSingleton(instance.AsComponentDataReference());
        }

        public void RemoveManagedRef<T>(T instance) where T : class, IComponentReferencable
        {
            Entity entity = GetSingletonEntity<ManagedReferenceComponent<T>>();
            EntityManager.DestroyEntity(entity);
        }
    }
}