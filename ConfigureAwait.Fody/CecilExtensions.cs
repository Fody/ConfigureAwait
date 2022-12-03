using Fody;
using Mono.Cecil;
using Mono.Cecil.Cil;

static class CecilExtensions
{
    public static bool IsIAsyncStateMachine(this TypeDefinition typeDefinition)
    {
        if (typeDefinition == null || !typeDefinition.HasInterfaces)
        {
            return false;
        }

        return typeDefinition.Interfaces
            .Any(x => x.InterfaceType.FullName == "System.Runtime.CompilerServices.IAsyncStateMachine");
    }

    public static bool IsCompilerGenerated(this ICustomAttributeProvider provider)
    {
        if (provider == null || !provider.HasCustomAttributes)
        {
            return false;
        }

        return provider.CustomAttributes
            .Any(a => a.AttributeType.FullName == "System.Runtime.CompilerServices.CompilerGeneratedAttribute");
    }

    public static void InsertBefore(this ILProcessor processor, Instruction target, params Instruction[] instructions)
    {
        foreach (var instruction in instructions)
        {
            processor.InsertBefore(target, instruction);
        }
    }

    public static bool IsAsyncStateMachineType(this ICustomAttributeProvider provider)
    {
        return provider.CustomAttributes
            .Any(a => a.AttributeType.FullName == "System.Runtime.CompilerServices.AsyncStateMachineAttribute");
    }

    public static TypeDefinition GetAsyncStateMachineType(this ICustomAttributeProvider provider)
    {
        var attribute = provider.CustomAttributes
            .FirstOrDefault(a => a.AttributeType.FullName == "System.Runtime.CompilerServices.AsyncStateMachineAttribute");

        return (TypeDefinition)attribute?.ConstructorArguments[0].Value;
    }

    public static CustomAttribute GetConfigureAwaitAttribute(this ICustomAttributeProvider value)
    {
        return value.CustomAttributes.FirstOrDefault(a => a.AttributeType.FullName == "Fody.ConfigureAwaitAttribute");
    }

    public static bool? GetConfigureAwaitConfig(this ICustomAttributeProvider value, bool? defaultValue = null)
    {
        var attribute = value.GetConfigureAwaitAttribute();
        if (attribute == null)
        {
            return defaultValue;
        }

        if (value is MethodDefinition method &&
            !method.IsAsyncStateMachineType())
        {
            throw new WeavingException($"ConfigureAwaitAttribute applied to non-async method '{method.FullName}'.");
        }
        return (bool?)attribute.ConstructorArguments[0].Value;

    }

    public static void RemoveAllCustomAttributes(this ICustomAttributeProvider definition)
    {
        var customAttributes = definition.CustomAttributes;

        var attributes = customAttributes.Where(x => x.AttributeType.FullName == "Fody.ConfigureAwaitAttribute").ToArray();

        foreach (var attribute in attributes)
        {
            customAttributes.Remove(attribute);
        }
    }

    public static GenericInstanceType MakeGenericInstanceType(this TypeReference self, IEnumerable<TypeReference> arguments)
    {
        var instance = new GenericInstanceType(self);
        foreach (var arg in arguments)
        {
            instance.GenericArguments.Add(arg);
        }

        return instance;
    }
}