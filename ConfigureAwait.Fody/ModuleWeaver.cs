using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Fody;

public class ModuleWeaver : BaseModuleWeaver
{
    TypeReference configuredTaskAwaitableTypeRef;
    TypeReference configuredTaskAwaiterTypeRef;
    TypeDefinition gConfiguredTaskAwaiterTypeDef;
    TypeDefinition gConfiguredTaskAwaitableTypeDef;
    TypeDefinition configuredTaskAwaitableTypeDef;
    TypeDefinition configuredTaskAwaiterTypeDef;
    TypeReference gConfiguredTaskAwaiterTypeRef;

    public override void Execute()
    {
        configuredTaskAwaitableTypeDef = FindType("System.Runtime.CompilerServices.ConfiguredTaskAwaitable");
        configuredTaskAwaiterTypeDef = configuredTaskAwaitableTypeDef.NestedTypes[0];
        configuredTaskAwaitableTypeRef = ModuleDefinition.ImportReference(configuredTaskAwaitableTypeDef);
        configuredTaskAwaiterTypeRef = ModuleDefinition.ImportReference(configuredTaskAwaiterTypeDef);

        gConfiguredTaskAwaitableTypeDef = FindType("System.Runtime.CompilerServices.ConfiguredTaskAwaitable`1");
        gConfiguredTaskAwaiterTypeDef = gConfiguredTaskAwaitableTypeDef.NestedTypes[0];
        gConfiguredTaskAwaiterTypeRef = ModuleDefinition.ImportReference(gConfiguredTaskAwaiterTypeDef);
        var configureAwaitValue = (bool?)ModuleDefinition.Assembly.GetConfigureAwaitAttribute()?.ConstructorArguments[0].Value;

        var types = ModuleDefinition.GetTypes()
            .ToList();

        foreach (var type in types)
        {
            ProcessType(configureAwaitValue, type);
        }

        RemoveAttributes(types);
    }

    public override bool ShouldCleanReference => true;

    void ProcessType(bool? assemblyConfigureAwaitValue, TypeDefinition type)
    {
        if (type.IsCompilerGenerated() && type.IsIAsyncStateMachine())
        {
            return;
        }

        var configureAwaitValue = (bool?)type.GetConfigureAwaitAttribute()?.ConstructorArguments[0].Value;
        configureAwaitValue = configureAwaitValue ?? assemblyConfigureAwaitValue;

        foreach (var method in type.Methods)
        {
            var localConfigureAwaitValue = (bool?)method.GetConfigureAwaitAttribute()?.ConstructorArguments[0].Value;
            var localConfigWasSet = localConfigureAwaitValue.HasValue;
            localConfigureAwaitValue = localConfigureAwaitValue ?? configureAwaitValue;
            if (localConfigureAwaitValue == null)
            {
                continue;
            }

            var asyncStateMachineType = method.GetAsyncStateMachineType();
            if (asyncStateMachineType != null)
            {
                AddAwaitConfigToAsyncMethod(asyncStateMachineType, localConfigureAwaitValue.Value);
            }
            else if (localConfigWasSet)
            {
                LogWarning($"ConfigureAwaitAttribute applied to non-async method '{method.FullName}'.");
                continue;
            }
        }
    }

    public override IEnumerable<string> GetAssembliesForScanning()
    {
        yield return "netstandard";
        yield return "mscorlib";
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

            // Change TaskAwaiter`1 to ConfiguredTaskAwaiter`1
            if (field.FieldType.IsGenericInstance)
            {
                var genericFieldType = (GenericInstanceType)field.FieldType;
                var fieldType = field.FieldType.Resolve();
                if (fieldType.FullName == "System.Runtime.CompilerServices.TaskAwaiter`1")
                {
                    var genericArguments = genericFieldType.GenericArguments;
                    field.FieldType = gConfiguredTaskAwaiterTypeRef.MakeGenericInstanceType(genericArguments);
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
            MethodReference configureAwaitMethod = null;
            // Change variable type
            if (v.VariableType.FullName == "System.Runtime.CompilerServices.TaskAwaiter")
            {
                v.VariableType = configuredTaskAwaiterTypeRef;
                awaitableVar = new VariableDefinition(configuredTaskAwaitableTypeRef);
                method.Body.Variables.Insert(i + 1, awaitableVar);

                var configureAwaitMethodDef = FindType("System.Threading.Tasks.Task").Methods.First(m => m.Name == "ConfigureAwait");
                configureAwaitMethod = ModuleDefinition.ImportReference(configureAwaitMethodDef);
            }

            if (v.VariableType.IsGenericInstance)
            {
                var genericVariableType = (GenericInstanceType)v.VariableType;
                var variableType = v.VariableType.Resolve();
                if (variableType.FullName == "System.Runtime.CompilerServices.TaskAwaiter`1")
                {
                    v.VariableType = gConfiguredTaskAwaiterTypeRef.MakeGenericInstanceType(genericVariableType.GenericArguments);
                    awaitableVar = new VariableDefinition(ModuleDefinition.ImportReference(gConfiguredTaskAwaitableTypeDef).MakeGenericInstanceType(genericVariableType.GenericArguments));
                    method.Body.Variables.Insert(i + 1, awaitableVar);

                    var configureAwaitMethodDef = FindType("System.Threading.Tasks.Task`1").Methods.First(m => m.Name == "ConfigureAwait");
                    configureAwaitMethod = ModuleDefinition.ImportReference(configureAwaitMethodDef);
                    configureAwaitMethod.DeclaringType = ModuleDefinition.ImportReference(FindType("System.Threading.Tasks.Task`1")).MakeGenericInstanceType(genericVariableType.GenericArguments);
                }
            }

            if (awaitableVar != null)
            {
                awaitAwaiterPair.Add(v, awaitableVar);
                configureAwaitMethods.Add(v, configureAwaitMethod);
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
                    var newOperand = gConfiguredTaskAwaitableTypeDef.Methods.FirstOrDefault(m => m.Name == methodRef.Name);
                    if (newOperand != null)
                    {
                        var genericArguments = ((GenericInstanceType)methodRef.DeclaringType).GenericArguments;

                        var newOperandRef = ModuleDefinition.ImportReference(newOperand);
                        newOperandRef.DeclaringType = ModuleDefinition.ImportReference(gConfiguredTaskAwaitableTypeDef).MakeGenericInstanceType(genericArguments);
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
                    var newOperand = gConfiguredTaskAwaiterTypeDef.Methods.FirstOrDefault(m => m.Name == methodRef.Name);
                    if (newOperand != null)
                    {
                        var genericArguments = ((GenericInstanceType)methodRef.DeclaringType).GenericArguments;

                        var newOperandRef = ModuleDefinition.ImportReference(newOperand);
                        newOperandRef.DeclaringType = gConfiguredTaskAwaiterTypeRef.MakeGenericInstanceType(genericArguments);
                        method.Body.Instructions[i] = Instruction.Create(OpCodes.Call, newOperandRef);
                    }
                }

                // Change AwaitUnsafeOnCompleted<TaskAwaiter, T> to AwaitUnsafeOnCompleted<ConfiguredTaskAwaiter, T>
                // Change AwaitUnsafeOnCompleted<TaskAwaiter`1, T> to AwaitUnsafeOnCompleted<ConfiguredTaskAwaiter`1, T>
                if (methodRef.IsGenericInstance && methodRef.Name == "AwaitUnsafeOnCompleted")
                {
                    var awaitUnsafeOnCompleted = (GenericInstanceMethod)methodRef;

                    for (var j = 0; j < awaitUnsafeOnCompleted.GenericArguments.Count; j++)
                    {
                        if (awaitUnsafeOnCompleted.GenericArguments[j].FullName == "System.Runtime.CompilerServices.TaskAwaiter")
                        {
                            awaitUnsafeOnCompleted.GenericArguments[j] = configuredTaskAwaiterTypeRef;
                        }

                        var theArg = awaitUnsafeOnCompleted.GenericArguments[j].Resolve();
                        if (theArg.FullName == "System.Runtime.CompilerServices.TaskAwaiter`1")
                        {
                            var genericArguments = ((GenericInstanceType)awaitUnsafeOnCompleted.GenericArguments[j]).GenericArguments;
                            awaitUnsafeOnCompleted.GenericArguments[j] = gConfiguredTaskAwaiterTypeRef.MakeGenericInstanceType(genericArguments);
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

                // Change TaskAwaiter`1 to ConfiguredTaskAwaiter`1
                var theType = typeRef.Resolve();
                if (theType?.FullName == "System.Runtime.CompilerServices.TaskAwaiter`1")
                {
                    var genericArguments = ((GenericInstanceType)typeRef).GenericArguments;
                    instruction.Operand = gConfiguredTaskAwaiterTypeRef.MakeGenericInstanceType(genericArguments);
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

                // Change TaskAwaiter`1 to ConfiguredTaskAwaiter`1
                if (fieldRef.FieldType.IsGenericInstance)
                {
                    var genericFieldType = (GenericInstanceType)fieldRef.FieldType;
                    var fieldType = fieldRef.FieldType.Resolve();
                    if (fieldType.FullName == "System.Runtime.CompilerServices.TaskAwaiter`1")
                    {
                        var genericArguments = genericFieldType.GenericArguments;
                        fieldRef.FieldType = gConfiguredTaskAwaiterTypeRef.MakeGenericInstanceType(genericArguments);
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
            return (method.DeclaringType.FullName == "System.Threading.Tasks.Task" || method.DeclaringType.Resolve().FullName == "System.Threading.Tasks.Task`1")
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