using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Anvil.CSharp.Logging;
using Anvil.Unity.DOTS.Jobs;
using System;
using Logger = Anvil.CSharp.Logging.Logger;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// The base class for Systems when using the Anvil Framework.
    /// Adds some convenience functionality non-release safety checks for
    /// <see cref="SystemBase"/> implementations.
    /// </summary>
    public abstract partial class AbstractAnvilSystemBase : SystemBase
    {
        private Logger? m_Logger;
        /// <summary>
        /// Returns a <see cref="Logger"/> for this instance to emit log messages with.
        /// Lazy instantiated.
        /// </summary>
        protected Logger Logger
        {
            get => m_Logger ?? (m_Logger = Log.GetLogger(this)).Value;
            set => m_Logger = value;
        }

#if ANVIL_DEBUG_SAFETY
        /// <inheritdoc cref="Dependency" />
        protected new JobHandle Dependency
        {
            get => base.Dependency;
            // Detects situations where the existing dependency is overwritten rather than chained or combined.
            set
            {
                if (!value.DependsOn(base.Dependency))
                {
                    throw new InvalidOperationException($"Dependency Chain Broken: Dependency Chain Broken: The incoming dependency does not contain the existing dependency in the chain.");
                }

                base.Dependency = value;
            }
        }
#endif

        /// <summary>
        /// Creates a new <see cref="AbstractAnvilSystemBase"/> instance.
        /// </summary>
        protected AbstractAnvilSystemBase() : base() { }
    }
}