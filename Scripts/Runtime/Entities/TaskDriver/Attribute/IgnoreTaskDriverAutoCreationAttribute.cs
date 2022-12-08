using System;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    //TODO: #63 - Expand support to Properties
    [AttributeUsage(AttributeTargets.Field)]
    public class IgnoreTaskDriverAutoCreationAttribute : Attribute
    {

    }
}
