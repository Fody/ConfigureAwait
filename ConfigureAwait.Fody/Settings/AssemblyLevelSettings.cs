using Mono.Cecil;
using ConfigureAwait.Fody.Extensions;

namespace ConfigureAwait.Fody.Settings
{
    public class AssemblyLevelSettings
    {
        public AssemblyLevelSettings(ICustomAttributeProvider customAttributeProvider)
        {
            AssemblyConfigureAwait = customAttributeProvider.GetConfigureAwaitAttributeValue();
            if (AssemblyConfigureAwait.HasValue)
                customAttributeProvider.RemoveConfigureAwaitAttribute();
        }

        protected AssemblyLevelSettings(AssemblyLevelSettings settings)
        {
            AssemblyConfigureAwait = settings.AssemblyConfigureAwait;
        }

        public bool? AssemblyConfigureAwait { get; }

        public virtual bool? GetConfigureAwait()
        {
            return AssemblyConfigureAwait;
        }
    }
}