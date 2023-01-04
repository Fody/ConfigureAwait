using System.Xml;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Fody;

public partial class ModuleWeaver : BaseModuleWeaver
{
    public override void Execute()
    {
        ReadConfig();

        FindTypes();

        var configureAwaitValue = ModuleDefinition.Assembly.GetConfigureAwaitConfig(continueOnCapturedContext);

        foreach (var type in ModuleDefinition.GetTypes())
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
        ProcessFields(type);

        // Modify MoveNext method
        var method = type.Methods.First(m => m.Name == "MoveNext");
        method.Body.SimplifyMacros();

        ProcessVariables(configureAwaitValue, method);

        foreach (var instruction in method.Body.Instructions)
        {
            if (instruction.Operand is MethodReference methodRef)
            {
                TryRedirectMethodInstruction(methodRef, instruction);
                continue;
            }

            if (instruction.Operand is TypeReference typeRef)
            {
                TryRedirectTypeInstruction(typeRef, instruction);
                continue;
            }

            if (instruction.Operand is FieldReference fieldRef)
            {
                TryRedirectFieldInstruction(fieldRef);
                continue;
            }
        }

        method.Body.OptimizeMacros();
    }

    void TryRedirectMethodInstruction(MethodReference method, Instruction instruction)
    {
        // Change Task to ConfiguredTaskAwaitable
        var declaringType = method.DeclaringType;
        if (declaringType.FullName == "System.Threading.Tasks.Task")
        {
            var newOperand = configuredTaskAwaitableTypeDef.Method(method);
            if (newOperand != null)
            {
                instruction.OpCode = OpCodes.Call;
                instruction.Operand = ModuleDefinition.ImportReference(newOperand);
            }

            return;
        }

        if (declaringType.FullName == "System.Threading.Tasks.ValueTask")
        {
            var newOperand = configuredValueTaskAwaitableTypeDef.Method(method);
            if (newOperand != null)
            {
                instruction.OpCode = OpCodes.Call;
                instruction.Operand = ModuleDefinition.ImportReference(newOperand);
            }

            return;
        }

        // Change TaskAwaiter to ConfiguredTaskAwaiter
        if (declaringType.FullName == "System.Runtime.CompilerServices.TaskAwaiter")
        {
            var newOperand = configuredTaskAwaiterTypeDef.Method(method);
            if (newOperand != null)
            {
                instruction.OpCode = OpCodes.Call;
                instruction.Operand = ModuleDefinition.ImportReference(newOperand);
            }

            return;
        }

        // Change TaskAwaiter to ConfiguredTaskAwaiter
        if (declaringType.FullName == "System.Runtime.CompilerServices.ValueTaskAwaiter")
        {
            var newOperand = configuredValueTaskAwaiterTypeDef.Method(method);
            if (newOperand != null)
            {
                instruction.OpCode = OpCodes.Call;
                instruction.Operand = ModuleDefinition.ImportReference(newOperand);
            }

            return;
        }

        if (declaringType is GenericInstanceType genericType)
        {
            var genericArguments = genericType.GenericArguments;
            // Change Task`1 to ConfiguredTaskAwaitable`1
            if (declaringType.FullName.StartsWith("System.Threading.Tasks.Task`1"))
            {
                var newOperand = genericConfiguredTaskAwaitableTypeDef.Method(method);
                if (newOperand != null)
                {
                    var newOperandRef = ModuleDefinition.ImportReference(newOperand);
                    newOperandRef.DeclaringType = genericConfiguredTaskAwaitableTypeRef.MakeGenericInstanceType(genericArguments);
                    instruction.OpCode = OpCodes.Call;
                    instruction.Operand = newOperandRef;
                }

                return;
            }

            // Change Task`1 to ConfiguredTaskAwaitable`1
            if (declaringType.FullName.StartsWith("System.Threading.Tasks.ValueTask`1"))
            {
                var newOperand = genericConfiguredValueTaskAwaitableTypeDef.Method(method);
                if (newOperand != null)
                {
                    var newOperandRef = ModuleDefinition.ImportReference(newOperand);
                    newOperandRef.DeclaringType = genericConfiguredValueTaskAwaitableTypeRef.MakeGenericInstanceType(genericArguments);
                    instruction.OpCode = OpCodes.Call;
                    instruction.Operand = newOperandRef;
                }

                return;
            }

            // Change TaskAwaiter`1 to ConfiguredTaskAwaiter`1
            if (declaringType.FullName.StartsWith("System.Runtime.CompilerServices.TaskAwaiter`1"))
            {
                var newOperand = genericConfiguredTaskAwaiterTypeDef.Method(method);
                if (newOperand != null)
                {
                    var newOperandRef = ModuleDefinition.ImportReference(newOperand);
                    newOperandRef.DeclaringType = genericConfiguredTaskAwaiterTypeRef.MakeGenericInstanceType(genericArguments);
                    instruction.OpCode = OpCodes.Call;
                    instruction.Operand = newOperandRef;
                }

                return;
            }

            // Change TaskAwaiter`1 to ConfiguredTaskAwaiter`1
            if (declaringType.FullName.StartsWith("System.Runtime.CompilerServices.ValueTaskAwaiter`1"))
            {
                var newOperand = genericConfiguredValueTaskAwaiterTypeDef.Method(method);
                if (newOperand != null)
                {
                    var newOperandRef = ModuleDefinition.ImportReference(newOperand);
                    newOperandRef.DeclaringType = genericConfiguredValueTaskAwaiterTypeRef.MakeGenericInstanceType(genericArguments);
                    instruction.OpCode = OpCodes.Call;
                    instruction.Operand = newOperandRef;
                }

                return;
            }
        }

        // Change AwaitUnsafeOnCompleted<TaskAwaiter, T> to AwaitUnsafeOnCompleted<ConfiguredTaskAwaiter, T>
        // Change AwaitUnsafeOnCompleted<TaskAwaiter`1, T> to AwaitUnsafeOnCompleted<ConfiguredTaskAwaiter`1, T>
        if (method.Name == "AwaitUnsafeOnCompleted" && method is GenericInstanceMethod awaitUnsafeOnCompleted)
        {
            var arguments = awaitUnsafeOnCompleted.GenericArguments;
            for (var index = 0; index < arguments.Count; index++)
            {
                var argument = arguments[index];
                var fullName = argument.FullName;

                if (fullName == "System.Runtime.CompilerServices.TaskAwaiter")
                {
                    arguments[index] = configuredTaskAwaiterTypeRef;
                    continue;
                }

                if (fullName == "System.Runtime.CompilerServices.ValueTaskAwaiter")
                {
                    arguments[index] = configuredValueTaskAwaiterTypeRef;
                    continue;
                }

                if (argument is GenericInstanceType genericInstanceType)
                {
                    var genericArguments = genericInstanceType.GenericArguments;
                    if (fullName.StartsWith("System.Runtime.CompilerServices.TaskAwaiter`1"))
                    {
                        arguments[index] = genericConfiguredTaskAwaiterTypeRef.MakeGenericInstanceType(genericArguments);
                        continue;
                    }

                    if (fullName.StartsWith("System.Runtime.CompilerServices.ValueTaskAwaiter`1"))
                    {
                        arguments[index] = genericConfiguredValueTaskAwaiterTypeRef.MakeGenericInstanceType(genericArguments);
                        continue;
                    }
                }
            }
        }
    }

    void TryRedirectTypeInstruction(TypeReference type, Instruction instruction)
    {
        // Change TaskAwaiter to ConfiguredTaskAwaiter
        var typeFullName = type.FullName;
        if (typeFullName == "System.Runtime.CompilerServices.TaskAwaiter")
        {
            instruction.Operand = configuredTaskAwaiterTypeRef;
            return;
        }

        if (typeFullName == "System.Runtime.CompilerServices.ValueTaskAwaiter")
        {
            instruction.Operand = configuredValueTaskAwaiterTypeRef;
            return;
        }

        // Change TaskAwaiter`1 to ConfiguredTaskAwaiter`1
        if (type is not GenericInstanceType genericType)
        {
            return;
        }

        var genericArguments = genericType.GenericArguments;

        if (genericType.FullName.StartsWith("System.Runtime.CompilerServices.TaskAwaiter`1"))
        {
            instruction.Operand = genericConfiguredTaskAwaiterTypeRef.MakeGenericInstanceType(genericArguments);
        }
        else if (genericType.FullName.StartsWith("System.Runtime.CompilerServices.ValueTaskAwaiter`1"))
        {
            instruction.Operand = genericConfiguredValueTaskAwaiterTypeRef.MakeGenericInstanceType(genericArguments);
        }
    }
}