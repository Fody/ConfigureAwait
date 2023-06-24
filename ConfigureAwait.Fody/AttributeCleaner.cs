using Mono.Cecil;

static class AttributeCleaner
{
    public static void Run(ModuleDefinition module)
    {
        module.Assembly.RemoveAllCustomAttributes();
        module.RemoveAllCustomAttributes();

        foreach (var typeDefinition in module.GetTypes())
        {
            typeDefinition.RemoveAllCustomAttributes();

            foreach (var method in typeDefinition.Methods)
            {
                method.RemoveAllCustomAttributes();
            }

            foreach (var property in typeDefinition.Properties)
            {
                property.RemoveAllCustomAttributes();
            }
        }
    }

    static void RemoveAllCustomAttributes(this ICustomAttributeProvider definition)
    {
        var customAttributes = definition.CustomAttributes;

        var attributes = customAttributes.Where(_ => _.AttributeType.FullName == "Fody.ConfigureAwaitAttribute").ToArray();

        foreach (var attribute in attributes)
        {
            customAttributes.Remove(attribute);
        }
    }
}