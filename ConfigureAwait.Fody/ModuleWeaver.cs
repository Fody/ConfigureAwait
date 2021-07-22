using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Fody;

public class ModuleWeaver : BaseModuleWeaver
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

        RemoveAttributes(types);
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

    void AddAwaitConfigToAsyncMethod(TypeDefinition type, bool value)
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
        var ilProcessor = method.Body.GetILProcessor();

        var awaitAwaiterPair = new Dictionary<VariableDefinition, VariableDefinition>();
        var configureAwaitMethods = new Dictionary<VariableDefinition, MethodReference>();
        var i = 0;

        foreach (var v in method.Body.Variables.ToArray())
        {
            VariableDefinition awaitableVar = null;
            MethodReference localConfigAwait = null;
            // Change variable type
            if (v.VariableType.FullName == "System.Runtime.CompilerServices.TaskAwaiter")
            {
                v.VariableType = configuredTaskAwaiterTypeRef;
                awaitableVar = new(configuredTaskAwaitableTypeRef);
                method.Body.Variables.Insert(i + 1, awaitableVar);

                localConfigAwait = taskConfigureAwaitMethod;
            }
            else if (v.VariableType.FullName == "System.Runtime.CompilerServices.ValueTaskAwaiter")
            {
                v.VariableType = configuredValueTaskAwaiterTypeRef;
                awaitableVar = new(configuredValueTaskAwaitableTypeRef);
                method.Body.Variables.Insert(i + 1, awaitableVar);

                localConfigAwait = valueTaskConfigureAwaitMethod;
            }

            if (v.VariableType.IsGenericInstance)
            {
                var genericVariableType = (GenericInstanceType)v.VariableType;
                var variableType = v.VariableType.Resolve();
                if (variableType.FullName == "System.Runtime.CompilerServices.TaskAwaiter`1")
                {
                    v.VariableType = genericConfiguredTaskAwaiterTypeRef.MakeGenericInstanceType(genericVariableType.GenericArguments);
                    awaitableVar = new(genericConfiguredTaskAwaitableTypeRef.MakeGenericInstanceType(genericVariableType.GenericArguments));
                    method.Body.Variables.Insert(i + 1, awaitableVar);

                    localConfigAwait = ModuleDefinition.ImportReference(genericTaskConfigureAwaitMethodDef);
                    localConfigAwait.DeclaringType = genericTaskType.MakeGenericInstanceType(genericVariableType.GenericArguments);
                }
                else if (variableType.FullName == "System.Runtime.CompilerServices.ValueTaskAwaiter`1")
                {
                    v.VariableType = genericConfiguredValueTaskAwaiterTypeRef.MakeGenericInstanceType(genericVariableType.GenericArguments);
                    awaitableVar = new(genericConfiguredValueTaskAwaitableTypeRef.MakeGenericInstanceType(genericVariableType.GenericArguments));
                    method.Body.Variables.Insert(i + 1, awaitableVar);

                    localConfigAwait = ModuleDefinition.ImportReference(genericValueTaskConfigureAwaitMethodDef);
                    localConfigAwait.DeclaringType = genericValueTaskType.MakeGenericInstanceType(genericVariableType.GenericArguments);
                }
            }

            if (awaitableVar != null)
            {
                awaitAwaiterPair.Add(v, awaitableVar);
                configureAwaitMethods.Add(v, localConfigAwait);
            }

            i++;
        }

        // Insert ConfigureAwait call just before GetAwaiter call.
        foreach (var instruction in method.Body.Instructions.Where(GetAwaiterSearch).ToList())
        {
            var variable = (VariableDefinition)instruction.Next.Operand;
            var awaitableVar = awaitAwaiterPair[variable];
            var configureAwait = configureAwaitMethods[variable];

            ilProcessor.InsertBefore(instruction,
                Instruction.Create(value ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0), // true or false
                Instruction.Create(OpCodes.Callvirt, configureAwait), // Call ConfigureAwait
                Instruction.Create(OpCodes.Stloc, awaitableVar), // Store in variable
                Instruction.Create(OpCodes.Ldloca, awaitableVar) // Load variable
            );
        }

        for (i = 0; i < method.Body.Instructions.Count; i++)
        {
            var instruction = method.Body.Instructions[i];

            if (instruction.Operand is MethodReference methodRef)
            {
                // Change Task to ConfiguredTaskAwaitable
                if (methodRef.DeclaringType.FullName == "System.Threading.Tasks.Task")
                {
                    var newOperand = configuredTaskAwaitableTypeDef.Methods.FirstOrDefault(m => m.Name == methodRef.Name);
                    if (newOperand != null)
                    {
                        method.Body.Instructions[i] = Instruction.Create(OpCodes.Call, ModuleDefinition.ImportReference(newOperand));
                    }
                }

                // Change Task`1 to ConfiguredTaskAwaitable`1
                if (methodRef.DeclaringType.Resolve().FullName == "System.Threading.Tasks.Task`1")
                {
                    var newOperand = genericConfiguredTaskAwaitableTypeDef.Methods.FirstOrDefault(m => m.Name == methodRef.Name);
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
                    var newOperand = configuredValueTaskAwaitableTypeDef.Methods.FirstOrDefault(m => m.Name == methodRef.Name);
                    if (newOperand != null)
                    {
                        method.Body.Instructions[i] = Instruction.Create(OpCodes.Call, ModuleDefinition.ImportReference(newOperand));
                    }
                }

                // Change Task`1 to ConfiguredTaskAwaitable`1
                if (methodRef.DeclaringType.Resolve().FullName == "System.Threading.Tasks.ValueTask`1")
                {
                    var newOperand = genericConfiguredValueTaskAwaitableTypeDef.Methods.FirstOrDefault(m => m.Name == methodRef.Name);
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
                    var newOperand = configuredTaskAwaiterTypeDef.Methods.FirstOrDefault(m => m.Name == methodRef.Name);
                    if (newOperand != null)
                    {
                        method.Body.Instructions[i] = Instruction.Create(OpCodes.Call, ModuleDefinition.ImportReference(newOperand));
                    }
                }

                // Change TaskAwaiter`1 to ConfiguredTaskAwaiter`1
                if (methodRef.DeclaringType.Resolve().FullName == "System.Runtime.CompilerServices.TaskAwaiter`1")
                {
                    var newOperand = genericConfiguredTaskAwaiterTypeDef.Methods.FirstOrDefault(m => m.Name == methodRef.Name);
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
                    var newOperand = configuredValueTaskAwaiterTypeDef.Methods.FirstOrDefault(m => m.Name == methodRef.Name);
                    if (newOperand != null)
                    {
                        method.Body.Instructions[i] = Instruction.Create(OpCodes.Call, ModuleDefinition.ImportReference(newOperand));
                    }
                }

                // Change TaskAwaiter`1 to ConfiguredTaskAwaiter`1
                if (methodRef.DeclaringType.Resolve().FullName == "System.Runtime.CompilerServices.ValueTaskAwaiter`1")
                {
                    var newOperand = genericConfiguredValueTaskAwaiterTypeDef.Methods.FirstOrDefault(m => m.Name == methodRef.Name);
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

    bool GetAwaiterSearch(Instruction instruction)
    {
        if (instruction.Operand is MethodReference method)
        {
            var declaringType = method.DeclaringType;
            return (
                       declaringType.FullName == "System.Threading.Tasks.Task" ||
                       declaringType.Resolve().FullName == "System.Threading.Tasks.Task`1" ||
                       declaringType.FullName == "System.Threading.Tasks.ValueTask" ||
                       declaringType.Resolve().FullName == "System.Threading.Tasks.ValueTask`1")
                   && method.Name == "GetAwaiter";
        }

        return false;
    }

    void RemoveAttributes(List<TypeDefinition> types)
    {
        ModuleDefinition.Assembly.RemoveAllCustomAttributes();
        ModuleDefinition.RemoveAllCustomAttributes();
        foreach (var typeDefinition in types)
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
}