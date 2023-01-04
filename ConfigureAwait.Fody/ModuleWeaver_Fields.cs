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

        if (field.FieldType.IsGenericInstance)
        {
            // Change TaskAwaiter`1 to ConfiguredTaskAwaiter`1
            var genericFieldType = (GenericInstanceType)field.FieldType;
            var fieldType = field.FieldType.Resolve();
            var genericArguments = genericFieldType.GenericArguments;

            if (fieldType.FullName == "System.Runtime.CompilerServices.TaskAwaiter`1")
            {
                field.FieldType = genericConfiguredTaskAwaiterTypeRef.MakeGenericInstanceType(genericArguments);
            }
            else if (fieldType.FullName == "System.Runtime.CompilerServices.ValueTaskAwaiter`1")
            {
                field.FieldType = genericConfiguredValueTaskAwaiterTypeRef.MakeGenericInstanceType(genericArguments);
            }
        }
    }

    void TryRedirectFieldInstruction(FieldReference fieldRef)
    {
        // Change TaskAwaiter to ConfiguredTaskAwaiter
        var typeFullName = fieldRef.FieldType.FullName;
        if (typeFullName == "System.Runtime.CompilerServices.TaskAwaiter")
        {
            fieldRef.FieldType = configuredTaskAwaiterTypeRef;
            return;
        }

        if (typeFullName == "System.Runtime.CompilerServices.ValueTaskAwaiter")
        {
            fieldRef.FieldType = configuredValueTaskAwaiterTypeRef;
            return;
        }

        // Change TaskAwaiter`1 to ConfiguredTaskAwaiter`1
        if (fieldRef.FieldType.IsGenericInstance)
        {
            var genericFieldType = (GenericInstanceType)fieldRef.FieldType;
            var fieldType = fieldRef.FieldType.Resolve();
            var genericArguments = genericFieldType.GenericArguments;

            if (fieldType.FullName == "System.Runtime.CompilerServices.TaskAwaiter`1")
            {
                fieldRef.FieldType = genericConfiguredTaskAwaiterTypeRef.MakeGenericInstanceType(genericArguments);
            }
            else if (fieldType.FullName == "System.Runtime.CompilerServices.ValueTaskAwaiter`1")
            {
                fieldRef.FieldType = genericConfiguredValueTaskAwaiterTypeRef.MakeGenericInstanceType(genericArguments);
            }
        }
    }
}