using System.Linq;
using Mono.Cecil;

namespace ConfigureAwait.Fody.Extensions
{
    public static class ConfigureAwaitAttributeExtensions
    {
        private const string ConfigureAwaitAttributeName = "Fody.ConfigureAwaitAttribute";

        public static bool? GetConfigureAwaitAttributeValue(this ICustomAttributeProvider value)
        {
            var attribute =
                value.CustomAttributes.FirstOrDefault(x => x.AttributeType.FullName == ConfigureAwaitAttributeName);
            return (bool?) attribute?.ConstructorArguments[0].Value;
        }

        public static void RemoveConfigureAwaitAttribute(this ICustomAttributeProvider definition)
        {
            for (var i = definition.CustomAttributes.Count - 1; i >= 0; i--)
            {
                var attribute = definition.CustomAttributes[i];
                if (attribute.AttributeType.FullName == ConfigureAwaitAttributeName)
                {
                    definition.CustomAttributes.Remove(attribute);
                    break;
                }
            }
        }
    }
}