using System;
using System.Linq;

namespace ConfigureAwait
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method)]
    public class ConfigureAwaitAttribute : Attribute
    {
        public ConfigureAwaitAttribute(bool continueOnCapturedContext)
        {
        }
    }
}