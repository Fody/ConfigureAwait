using System.Xml;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Fody;

public partial class ModuleWeaver : BaseModuleWeaver
{
    TypeReference configuredTaskAwaitableTypeRef;
    TypeReference configuredValueTaskAwaitableTypeRef;
    TypeReference configuredTaskAwaiterTypeRef;
    TypeReference configuredValueTaskAwaiterTypeRef;
    TypeDefinition genericConfiguredTaskAwaiterTypeDef;
    TypeDefinition genericConfiguredValueTaskAwaiterTypeDef;
    TypeDefinition genericConfiguredTaskAwaitableTypeDef;
    TypeDefinition genericConfiguredValueTaskAwaitableTypeDef;
    TypeDefinition configuredTaskAwaitableTypeDef;
    TypeDefinition configuredValueTaskAwaitableTypeDef;
    TypeDefinition configuredTaskAwaiterTypeDef;
    TypeDefinition configuredValueTaskAwaiterTypeDef;
    TypeReference genericConfiguredTaskAwaiterTypeRef;
    TypeReference genericConfiguredValueTaskAwaiterTypeRef;
    MethodDefinition genericTaskConfigureAwaitMethodDef;
    MethodDefinition genericValueTaskConfigureAwaitMethodDef;
    TypeDefinition taskDef;
    TypeDefinition valueTaskDef;
    MethodReference taskConfigureAwaitMethod;
    MethodReference valueTaskConfigureAwaitMethod;
    TypeReference genericConfiguredTaskAwaitableTypeRef;
    TypeReference genericConfiguredValueTaskAwaitableTypeRef;
    TypeReference genericTaskType;
    TypeReference genericValueTaskType;

    public override void Execute()
    {
        ReadConfig();

        taskDef = FindTypeDefinition("System.Threading.Tasks.Task");
        var configureTaskAwaitMethodDef = taskDef.Methods.First(m => m.Name == "ConfigureAwait");
        taskConfigureAwaitMethod = ModuleDefinition.ImportReference(configureTaskAwaitMethodDef);
        configuredTaskAwaitableTypeDef = FindTypeDefinition("System.Runtime.CompilerServices.ConfiguredTaskAwaitable");
        configuredTaskAwaiterTypeDef = configuredTaskAwaitableTypeDef.NestedTypes[0];
        configuredTaskAwaitableTypeRef = ModuleDefinition.ImportReference(configuredTaskAwaitableTypeDef);
        configuredTaskAwaiterTypeRef = ModuleDefinition.ImportReference(configuredTaskAwaiterTypeDef);

        var genericTaskDef = FindTypeDefinition("System.Threading.Tasks.Task`1");
        genericTaskConfigureAwaitMethodDef = genericTaskDef.Methods.First(m => m.Name == "ConfigureAwait");
        genericConfiguredTaskAwaitableTypeDef = FindTypeDefinition("System.Runtime.CompilerServices.ConfiguredTaskAwaitable`1");
        genericConfiguredTaskAwaiterTypeDef = genericConfiguredTaskAwaitableTypeDef.NestedTypes[0];
        genericConfiguredTaskAwaiterTypeRef = ModuleDefinition.ImportReference(genericConfiguredTaskAwaiterTypeDef);
        genericConfiguredTaskAwaitableTypeRef = ModuleDefinition.ImportReference(genericConfiguredTaskAwaitableTypeDef);
        genericTaskType = ModuleDefinition.ImportReference(genericTaskDef);

        if (TryFindTypeDefinition("System.Threading.Tasks.ValueTask", out valueTaskDef))
        {
            var configureValueTaskAwaitMethodDef = valueTaskDef.Methods.First(m => m.Name == "ConfigureAwait");
            valueTaskConfigureAwaitMethod = ModuleDefinition.ImportReference(configureValueTaskAwaitMethodDef);
            configuredValueTaskAwaitableTypeDef = FindTypeDefinition("System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable");
            configuredValueTaskAwaiterTypeDef = configuredValueTaskAwaitableTypeDef.NestedTypes[0];
            configuredValueTaskAwaitableTypeRef = ModuleDefinition.ImportReference(configuredValueTaskAwaitableTypeDef);
            configuredValueTaskAwaiterTypeRef = ModuleDefinition.ImportReference(configuredValueTaskAwaiterTypeDef);
        }

        if (TryFindTypeDefinition("System.Threading.Tasks.ValueTask`1", out var genericValueTaskDef))
        {
            genericValueTaskConfigureAwaitMethodDef = genericValueTaskDef.Methods.First(m => m.Name == "ConfigureAwait");
            genericConfiguredValueTaskAwaitableTypeDef = FindTypeDefinition("System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable`1");
            genericConfiguredValueTaskAwaiterTypeDef = genericConfiguredValueTaskAwaitableTypeDef.NestedTypes[0];
            genericConfiguredValueTaskAwaiterTypeRef = ModuleDefinition.ImportReference(genericConfiguredValueTaskAwaiterTypeDef);
            genericConfiguredValueTaskAwaitableTypeRef = ModuleDefinition.ImportReference(genericConfiguredValueTaskAwaitableTypeDef);
            genericValueTaskType = ModuleDefinition.ImportReference(genericValueTaskDef);
        }

        var configureAwaitValue = ModuleDefinition.Assembly.GetConfigureAwaitConfig(continueOnCapturedContext);

        var types = ModuleDefinition.GetTypes().ToList();

        foreach (var type in types)
        {
            ProcessType(configureAwaitValue, type);
        }

        AttributeCleaner.Run(ModuleDefinition);
    }

    void ReadConfig()
    {
        var value = Config?.Attribute("ContinueOnCapturedContext")?.Value;
        if (value == null)
        {
            return;
        }

        try
        {
            continueOnCapturedContext = XmlConvert.ToBoolean(value.ToLowerInvariant());
        }
        catch
        {
            throw new WeavingException($"Could not parse 'ContinueOnCapturedContext' from '{value}'.");
        }
    }

    bool? continueOnCapturedContext;

    public override bool ShouldCleanReference => true;

    void ProcessType(bool? assemblyConfigureAwaitValue, TypeDefinition type)
    {
        if (type.IsCompilerGenerated() && type.IsIAsyncStateMachine())
        {
            return;
        }

        var configureAwaitValue = type.GetConfigureAwaitConfig(assemblyConfigureAwaitValue);

        foreach (var method in type.Methods)
        {
            var localConfigureAwaitValue = method.GetConfigureAwaitConfig(configureAwaitValue);
            if (localConfigureAwaitValue == null)
            {
                continue;
            }

            var asyncStateMachineType = method.GetAsyncStateMachineType();
            if (asyncStateMachineType != null)
            {
                AddAwaitConfigToAsyncMethod(asyncStateMachineType, localConfigureAwaitValue.Value);
            }
        }
    }

    public override IEnumerable<string> GetAssembliesForScanning()
    {
        yield return "System.Threading.Tasks.Extensions";
    }

    void AddAwaitConfigToAsyncMethod(TypeDefinition type, bool configureAwaitValue)
    {
        foreach (var field in type.Fields)
        {
            // Change TaskAwaiter to ConfiguredTaskAwaiter
            if (field.FieldType.FullName == "System.Runtime.CompilerServices.TaskAwaiter")
            {
                field.FieldType = configuredTaskAwaiterTypeRef;
            }
            else if (field.FieldType.FullName == "System.Runtime.CompilerServices.ValueTaskAwaiter")
            {
                field.FieldType = configuredValueTaskAwaiterTypeRef;
            }

            // Change TaskAwaiter`1 to ConfiguredTaskAwaiter`1
            if (field.FieldType.IsGenericInstance)
            {
                var genericFieldType = (GenericInstanceType)field.FieldType;
                var fieldType = field.FieldType.Resolve();
                if (fieldType.FullName == "System.Runtime.CompilerServices.TaskAwaiter`1")
                {
                    var genericArguments = genericFieldType.GenericArguments;
                    field.FieldType = genericConfiguredTaskAwaiterTypeRef.MakeGenericInstanceType(genericArguments);
                }
                else if (fieldType.FullName == "System.Runtime.CompilerServices.ValueTaskAwaiter`1")
                {
                    var genericArguments = genericFieldType.GenericArguments;
                    field.FieldType = genericConfiguredValueTaskAwaiterTypeRef.MakeGenericInstanceType(genericArguments);
                }
            }
        }

        // Modify MoveNext method
        var method = type.Methods.First(m => m.Name == "MoveNext");
        method.Body.SimplifyMacros();

        ProcessVariables(configureAwaitValue, method);

        for (var i = 0; i < method.Body.Instructions.Count; i++)
        {
            var instruction = method.Body.Instructions[i];

            if (instruction.Operand is MethodReference methodRef)
            {
                // Change Task to ConfiguredTaskAwaitable
                if (methodRef.DeclaringType.FullName == "System.Threading.Tasks.Task")
                {
                    var newOperand = configuredTaskAwaitableTypeDef.Method(methodRef);
                    if (newOperand != null)
                    {
                        method.Body.Instructions[i] = Instruction.Create(OpCodes.Call, ModuleDefinition.ImportReference(newOperand));
                    }
                }

                // Change Task`1 to ConfiguredTaskAwaitable`1
                if (methodRef.DeclaringType.Resolve().FullName == "System.Threading.Tasks.Task`1")
                {
                    var newOperand = genericConfiguredTaskAwaitableTypeDef.Method(methodRef);
                    if (newOperand != null)
                    {
                        var genericArguments = ((GenericInstanceType)methodRef.DeclaringType).GenericArguments;

                        var newOperandRef = ModuleDefinition.ImportReference(newOperand);
                        newOperandRef.DeclaringType = genericConfiguredTaskAwaitableTypeRef.MakeGenericInstanceType(genericArguments);
                        method.Body.Instructions[i] = Instruction.Create(OpCodes.Call, newOperandRef);
                    }
                }

                if (methodRef.DeclaringType.FullName == "System.Threading.Tasks.ValueTask")
                {
                    var newOperand = configuredValueTaskAwaitableTypeDef.Method(methodRef);
                    if (newOperand != null)
                    {
                        method.Body.Instructions[i] = Instruction.Create(OpCodes.Call, ModuleDefinition.ImportReference(newOperand));
                    }
                }

                // Change Task`1 to ConfiguredTaskAwaitable`1
                if (methodRef.DeclaringType.Resolve().FullName == "System.Threading.Tasks.ValueTask`1")
                {
                    var newOperand = genericConfiguredValueTaskAwaitableTypeDef.Method(methodRef);
                    if (newOperand != null)
                    {
                        var genericArguments = ((GenericInstanceType)methodRef.DeclaringType).GenericArguments;

                        var newOperandRef = ModuleDefinition.ImportReference(newOperand);
                        newOperandRef.DeclaringType = genericConfiguredValueTaskAwaitableTypeRef.MakeGenericInstanceType(genericArguments);
                        method.Body.Instructions[i] = Instruction.Create(OpCodes.Call, newOperandRef);
                    }
                }

                // Change TaskAwaiter to ConfiguredTaskAwaiter
                if (methodRef.DeclaringType.FullName == "System.Runtime.CompilerServices.TaskAwaiter")
                {
                    var newOperand = configuredTaskAwaiterTypeDef.Method(methodRef);
                    if (newOperand != null)
                    {
                        method.Body.Instructions[i] = Instruction.Create(OpCodes.Call, ModuleDefinition.ImportReference(newOperand));
                    }
                }

                // Change TaskAwaiter`1 to ConfiguredTaskAwaiter`1
                if (methodRef.DeclaringType.Resolve().FullName == "System.Runtime.CompilerServices.TaskAwaiter`1")
                {
                    var newOperand = genericConfiguredTaskAwaiterTypeDef.Method(methodRef);
                    if (newOperand != null)
                    {
                        var genericArguments = ((GenericInstanceType)methodRef.DeclaringType).GenericArguments;

                        var newOperandRef = ModuleDefinition.ImportReference(newOperand);
                        newOperandRef.DeclaringType = genericConfiguredTaskAwaiterTypeRef.MakeGenericInstanceType(genericArguments);
                        method.Body.Instructions[i] = Instruction.Create(OpCodes.Call, newOperandRef);
                    }
                }

                // Change TaskAwaiter to ConfiguredTaskAwaiter
                if (methodRef.DeclaringType.FullName == "System.Runtime.CompilerServices.ValueTaskAwaiter")
                {
                    var newOperand = configuredValueTaskAwaiterTypeDef.Method(methodRef);
                    if (newOperand != null)
                    {
                        method.Body.Instructions[i] = Instruction.Create(OpCodes.Call, ModuleDefinition.ImportReference(newOperand));
                    }
                }

                // Change TaskAwaiter`1 to ConfiguredTaskAwaiter`1
                if (methodRef.DeclaringType.Resolve().FullName == "System.Runtime.CompilerServices.ValueTaskAwaiter`1")
                {
                    var newOperand = genericConfiguredValueTaskAwaiterTypeDef.Method(methodRef);
                    if (newOperand != null)
                    {
                        var genericArguments = ((GenericInstanceType)methodRef.DeclaringType).GenericArguments;

                        var newOperandRef = ModuleDefinition.ImportReference(newOperand);
                        newOperandRef.DeclaringType = genericConfiguredValueTaskAwaiterTypeRef.MakeGenericInstanceType(genericArguments);
                        method.Body.Instructions[i] = Instruction.Create(OpCodes.Call, newOperandRef);
                    }
                }

                // Change AwaitUnsafeOnCompleted<TaskAwaiter, T> to AwaitUnsafeOnCompleted<ConfiguredTaskAwaiter, T>
                // Change AwaitUnsafeOnCompleted<TaskAwaiter`1, T> to AwaitUnsafeOnCompleted<ConfiguredTaskAwaiter`1, T>
                if (methodRef.IsGenericInstance && methodRef.Name == "AwaitUnsafeOnCompleted")
                {
                    var awaitUnsafeOnCompleted = (GenericInstanceMethod)methodRef;

                    var arguments = awaitUnsafeOnCompleted.GenericArguments;
                    for (var j = 0; j < arguments.Count; j++)
                    {
                        if (arguments[j].FullName == "System.Runtime.CompilerServices.TaskAwaiter")
                        {
                            arguments[j] = configuredTaskAwaiterTypeRef;
                        }
                        else if (arguments[j].FullName == "System.Runtime.CompilerServices.ValueTaskAwaiter")
                        {
                            arguments[j] = configuredValueTaskAwaiterTypeRef;
                        }

                        var theArg = arguments[j].Resolve();
                        if (theArg.FullName == "System.Runtime.CompilerServices.TaskAwaiter`1")
                        {
                            var genericArguments = ((GenericInstanceType)arguments[j]).GenericArguments;
                            arguments[j] = genericConfiguredTaskAwaiterTypeRef.MakeGenericInstanceType(genericArguments);
                        }
                        else if (theArg.FullName == "System.Runtime.CompilerServices.ValueTaskAwaiter`1")
                        {
                            var genericArguments = ((GenericInstanceType)arguments[j]).GenericArguments;
                            arguments[j] = genericConfiguredValueTaskAwaiterTypeRef.MakeGenericInstanceType(genericArguments);
                        }
                    }
                }
            }

            if (instruction.Operand is TypeReference typeRef)
            {
                // Change TaskAwaiter to ConfiguredTaskAwaiter
                var typeFullName = typeRef.FullName;
                if (typeFullName == "System.Runtime.CompilerServices.TaskAwaiter")
                {
                    instruction.Operand = configuredTaskAwaiterTypeRef;
                }
                else if (typeFullName == "System.Runtime.CompilerServices.ValueTaskAwaiter")
                {
                    instruction.Operand = configuredValueTaskAwaiterTypeRef;
                }

                // Change TaskAwaiter`1 to ConfiguredTaskAwaiter`1
                var theType = typeRef.Resolve();
                if (theType?.FullName == "System.Runtime.CompilerServices.TaskAwaiter`1")
                {
                    var genericArguments = ((GenericInstanceType)typeRef).GenericArguments;
                    instruction.Operand = genericConfiguredTaskAwaiterTypeRef.MakeGenericInstanceType(genericArguments);
                }
                else if (theType?.FullName == "System.Runtime.CompilerServices.ValueTaskAwaiter`1")
                {
                    var genericArguments = ((GenericInstanceType)typeRef).GenericArguments;
                    instruction.Operand = genericConfiguredValueTaskAwaiterTypeRef.MakeGenericInstanceType(genericArguments);
                }
            }

            if (instruction.Operand is FieldReference fieldRef)
            {
                // Change TaskAwaiter to ConfiguredTaskAwaiter
                var typeFullName = fieldRef.FieldType.FullName;
                if (typeFullName == "System.Runtime.CompilerServices.TaskAwaiter")
                {
                    fieldRef.FieldType = configuredTaskAwaiterTypeRef;
                }
                else if (typeFullName == "System.Runtime.CompilerServices.ValueTaskAwaiter")
                {
                    fieldRef.FieldType = configuredValueTaskAwaiterTypeRef;
                }

                // Change TaskAwaiter`1 to ConfiguredTaskAwaiter`1
                if (fieldRef.FieldType.IsGenericInstance)
                {
                    var genericFieldType = (GenericInstanceType)fieldRef.FieldType;
                    var fieldType = fieldRef.FieldType.Resolve();
                    if (fieldType.FullName == "System.Runtime.CompilerServices.TaskAwaiter`1")
                    {
                        var genericArguments = genericFieldType.GenericArguments;
                        fieldRef.FieldType = genericConfiguredTaskAwaiterTypeRef.MakeGenericInstanceType(genericArguments);
                    }
                    else if (fieldType.FullName == "System.Runtime.CompilerServices.ValueTaskAwaiter`1")
                    {
                        var genericArguments = genericFieldType.GenericArguments;
                        fieldRef.FieldType = genericConfiguredValueTaskAwaiterTypeRef.MakeGenericInstanceType(genericArguments);
                    }
                }
            }
        }

        method.Body.OptimizeMacros();
    }
}