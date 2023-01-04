using Mono.Cecil;

public partial class ModuleWeaver
{
    void ProcessFields(TypeDefinition type)
    {
        foreach (var field in type.Fields)
        {
            ProcessField(field);
        }
    }

    void ProcessField(FieldDefinition field)
    {
        // Change TaskAwaiter to ConfiguredTaskAwaiter
        if (field.FieldType.FullName == "System.Runtime.CompilerServices.TaskAwaiter")
        {
            field.FieldType = configuredTaskAwaiterTypeRef;
            return;
        }

        if (field.FieldType.FullName == "System.Runtime.CompilerServices.ValueTaskAwaiter")
        {
            field.FieldType = configuredValueTaskAwaiterTypeRef;
            return;
        }

        if (field.FieldType is not GenericInstanceType genericType)
        {
            return;
        }

        // Change TaskAwaiter`1 to ConfiguredTaskAwaiter`1
        var genericArguments = genericType.GenericArguments;

        if (genericType.FullName.StartsWith("System.Runtime.CompilerServices.TaskAwaiter`1"))
        {
            field.FieldType = genericConfiguredTaskAwaiterTypeRef.MakeGenericInstanceType(genericArguments);
        }
        else if (genericType.FullName.StartsWith("System.Runtime.CompilerServices.ValueTaskAwaiter`1"))
        {
            field.FieldType = genericConfiguredValueTaskAwaiterTypeRef.MakeGenericInstanceType(genericArguments);
        }
    }

    void TryRedirectFieldInstruction(FieldReference field)
    {
        // Change TaskAwaiter to ConfiguredTaskAwaiter
        var typeFullName = field.FieldType.FullName;
        if (typeFullName == "System.Runtime.CompilerServices.TaskAwaiter")
        {
            field.FieldType = configuredTaskAwaiterTypeRef;
            return;
        }

        if (typeFullName == "System.Runtime.CompilerServices.ValueTaskAwaiter")
        {
            field.FieldType = configuredValueTaskAwaiterTypeRef;
            return;
        }

        // Change TaskAwaiter`1 to ConfiguredTaskAwaiter`1
        if (field.FieldType is not GenericInstanceType genericType)
        {
            return;
        }

        var genericArguments = genericType.GenericArguments;

        if (genericType.FullName.StartsWith("System.Runtime.CompilerServices.TaskAwaiter`1"))
        {
            field.FieldType = genericConfiguredTaskAwaiterTypeRef.MakeGenericInstanceType(genericArguments);
        }
        else if (genericType.FullName.StartsWith("System.Runtime.CompilerServices.ValueTaskAwaiter`1"))
        {
            field.FieldType = genericConfiguredValueTaskAwaiterTypeRef.MakeGenericInstanceType(genericArguments);
        }
    }
}