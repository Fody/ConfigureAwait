using System;

namespace Fody
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method)]
    public class ConfigureAwaitAttribute : Attribute
    {
        public ConfigureAwaitAttribute(bool continueOnCapturedContext)
        {
        }
    }
}