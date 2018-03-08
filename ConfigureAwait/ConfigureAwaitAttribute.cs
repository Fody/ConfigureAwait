using System;

namespace Fody
{
    /// <summary>
    /// Controls the config of the ConfigureAwait fody weaver.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method)]
    public class ConfigureAwaitAttribute : Attribute
    {
        /// <summary>
        /// Instantiate a new instance of <see cref="ConfigureAwaitAttribute"/>.
        /// </summary>
        /// <param name="continueOnCapturedContext">true to continue on CapturedContext.</param>
        public ConfigureAwaitAttribute(bool continueOnCapturedContext)
        {
        }
    }
}