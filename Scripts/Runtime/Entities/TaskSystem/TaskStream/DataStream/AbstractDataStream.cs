using Anvil.CSharp.Core;
using Anvil.CSharp.Reflection;
using Anvil.Unity.DOTS.Jobs;
using System;
using Anvil.CSharp.Logging;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    public abstract class AbstractDataStream : AbstractAnvilBase
    {
        public Type Type { get; }

        internal AccessController AccessController { get; }

        protected AbstractDataStream()
        {
            Type = GetType();
            AccessController = new AccessController();
        }

        protected override void DisposeSelf()
        {
            AccessController.Dispose();
            base.DisposeSelf();
        }

        public override string ToString()
        {
            return Type.GetReadableName();
        }
    }
}