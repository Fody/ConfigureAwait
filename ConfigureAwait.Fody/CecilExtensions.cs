static class CecilExtensions
{
    // Not yet defined in Cecil, remove later when update is available.
    const MethodImplAttributes MethodImplAttributes_Async = (MethodImplAttributes)0x2000;

    public static bool IsIAsyncStateMachine(this TypeDefinition type)
    {
        if (type is not {HasInterfaces: true})
        {
            return false;
        }

        return type.Interfaces
            .Any(_ => _.InterfaceType.FullName == "System.Runtime.CompilerServices.IAsyncStateMachine");
    }

    public static MethodDefinition Method(this TypeDefinition type, MethodReference reference)
    {
        return type.Methods.FirstOrDefault(item => item.Name == reference.Name);
    }

    public static bool IsCompilerGenerated(this ICustomAttributeProvider provider)
    {
        if (provider is not {HasCustomAttributes: true})
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

    // Async iterators (async IAsyncEnumerable<T> with yield) are lowered to a state machine
    // just like normal async methods, but the generated method carries
    // AsyncIteratorStateMachineAttribute rather than AsyncStateMachineAttribute.
    static bool IsAsyncStateMachineAttribute(this CustomAttribute attribute)
    {
        var fullName = attribute.AttributeType.FullName;
        return fullName is
            "System.Runtime.CompilerServices.AsyncStateMachineAttribute" or
            "System.Runtime.CompilerServices.AsyncIteratorStateMachineAttribute";
    }

    public static AsyncStateMachineKind GetAsyncStateMachineKind(this MethodDefinition method)
    {
        if (method.CustomAttributes.Any(IsAsyncStateMachineAttribute))
        {
            return AsyncStateMachineKind.StateMachine;
        }

        if (method.ImplAttributes.HasFlag(MethodImplAttributes_Async))
        {
            return AsyncStateMachineKind.CompilerService;
        }

        return AsyncStateMachineKind.None;
    }

    public static TypeDefinition GetAsyncStateMachineType(this ICustomAttributeProvider provider)
    {
        var attribute = provider.CustomAttributes
            .FirstOrDefault(IsAsyncStateMachineAttribute);

        return (TypeDefinition)attribute?.ConstructorArguments[0].Value;
    }

    static CustomAttribute GetConfigureAwaitAttribute(this ICustomAttributeProvider value)
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

        if (value is MethodDefinition method && method.GetAsyncStateMachineKind() == AsyncStateMachineKind.None)
        {
            throw new WeavingException($"ConfigureAwaitAttribute applied to non-async method '{method.FullName}'.");
        }

        return (bool?)attribute.ConstructorArguments[0].Value;
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