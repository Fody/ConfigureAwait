using System.Collections.Generic;
using System.Linq;
using ConfigureAwait.Fody.Extensions;
using ConfigureAwait.Fody.Utilities;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace GlobalConfigureAwait.Extensions
{
    public class AsyncIlHelper
    {
        private readonly TypeDefinition _configuredTaskAwaitableType;
        private readonly TypeReference _configuredTaskAwaitableTypeRef;
        private readonly TypeDefinition _configuredTaskAwaiterType;
        private readonly TypeReference _configuredTaskAwaiterTypeRef;
        private readonly TypeDefinition _genericConfiguredTaskAwaitableType;
        private readonly TypeDefinition _genericConfiguredTaskAwaiterType;
        private readonly MethodReference _genericTaskConfigureAwaitMethod;
        private readonly TypeDefinition _genericTaskType;
        private readonly ModuleDefinition _moduleDefinition;
        private readonly MethodReference _taskConfigureAwaitMethod;

        public AsyncIlHelper(TypeProvider typeProvider, TypeReferenceProvider typeReferenceProvider,
            ModuleDefinition moduleDefinition)
        {
            _moduleDefinition = moduleDefinition;

            //Awaitable
            _configuredTaskAwaitableTypeRef = typeReferenceProvider.ConfiguredTaskAwaitableReference;
            _configuredTaskAwaitableType = typeProvider.ConfiguredTaskAwaitableDefinition;

            _configuredTaskAwaiterTypeRef = typeReferenceProvider.ConfiguredTaskAwaiterReference;
            _configuredTaskAwaiterType = typeProvider.ConfiguredTaskAwaiterDefinition;

            _genericConfiguredTaskAwaitableType = typeProvider.GenericConfiguredTaskAwaitableDefinition;
            _genericConfiguredTaskAwaiterType = typeProvider.GenericConfiguredTaskAwaiterDefinition;

            _taskConfigureAwaitMethod = typeReferenceProvider.TaskConfigureAwaitMethodReference;

            _genericTaskType = typeProvider.GenericTaskDefinition;
            _genericTaskConfigureAwaitMethod = typeProvider.GenericTaskConfigureAwaitMethodDefinition;
        }

        public void AddAwaitConfigToAsyncMethod(TypeDefinition type, bool value)
        {
            foreach (var field in type.Fields)
            {
                // Change TaskAwaiter to ConfiguredTaskAwaiter
                if (field.FieldType.FullName == "System.Runtime.CompilerServices.TaskAwaiter")
                    field.FieldType = _configuredTaskAwaiterTypeRef;
                // Change TaskAwaiter`1 to ConfiguredTaskAwaiter`1
                if (field.FieldType.IsGenericInstance)
                {
                    var genericFieldType = (GenericInstanceType)field.FieldType;
                    var fieldtype = field.FieldType.Resolve();
                    if (fieldtype.FullName == "System.Runtime.CompilerServices.TaskAwaiter`1")
                    {
                        var genericArguments = genericFieldType.GenericArguments;
                        field.FieldType = _moduleDefinition.ImportReference(_genericConfiguredTaskAwaiterType)
                            .MakeGenericInstanceType(genericArguments.ToArray());
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
                    v.VariableType = _configuredTaskAwaiterTypeRef;
                    awaitableVar = new VariableDefinition(_configuredTaskAwaitableTypeRef);
                    method.Body.Variables.Insert(i + 1, awaitableVar);

                    var configureAwaitMethodDef = _taskConfigureAwaitMethod;
                    configureAwaitMethod = _moduleDefinition.ImportReference(configureAwaitMethodDef);
                }
                if (v.VariableType.IsGenericInstance)
                {
                    var genericVariableType = (GenericInstanceType)v.VariableType;
                    var variableType = v.VariableType.Resolve();
                    if (variableType.FullName == "System.Runtime.CompilerServices.TaskAwaiter`1")
                    {
                        v.VariableType = _moduleDefinition.ImportReference(_genericConfiguredTaskAwaiterType)
                            .MakeGenericInstanceType(genericVariableType.GenericArguments.ToArray());
                        awaitableVar = new VariableDefinition(_moduleDefinition
                            .ImportReference(_genericConfiguredTaskAwaitableType)
                            .MakeGenericInstanceType(genericVariableType.GenericArguments.ToArray()));
                        method.Body.Variables.Insert(i + 1, awaitableVar);

                        var configureAwaitMethodDef = _genericTaskConfigureAwaitMethod;
                        configureAwaitMethod = _moduleDefinition.ImportReference(configureAwaitMethodDef);
                        configureAwaitMethod.DeclaringType = _moduleDefinition.ImportReference(_genericTaskType)
                            .MakeGenericInstanceType(genericVariableType.GenericArguments.ToArray());
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
                        var newOperand =
                            _configuredTaskAwaitableType.Methods.FirstOrDefault(m => m.Name == methodRef.Name);
                        if (newOperand != null)
                            method.Body.Instructions[i] = Instruction.Create(OpCodes.Call,
                                _moduleDefinition.ImportReference(newOperand));
                    }

                    // Change Task`1 to ConfiguredTaskAwaitable`1
                    if (methodRef.DeclaringType.Resolve().FullName == "System.Threading.Tasks.Task`1")
                    {
                        var newOperand =
                            _genericConfiguredTaskAwaitableType.Methods.FirstOrDefault(m => m.Name == methodRef.Name);
                        if (newOperand != null)
                        {
                            var genericArguments = ((GenericInstanceType)methodRef.DeclaringType).GenericArguments;

                            var newOperandRef = _moduleDefinition.ImportReference(newOperand);
                            newOperandRef.DeclaringType = _moduleDefinition
                                .ImportReference(_genericConfiguredTaskAwaitableType)
                                .MakeGenericInstanceType(genericArguments.ToArray());
                            method.Body.Instructions[i] = Instruction.Create(OpCodes.Call, newOperandRef);
                        }
                    }

                    // Change TaskAwaiter to ConfiguredTaskAwaiter
                    if (methodRef.DeclaringType.FullName == "System.Runtime.CompilerServices.TaskAwaiter")
                    {
                        var newOperand =
                            _configuredTaskAwaiterType.Methods.FirstOrDefault(m => m.Name == methodRef.Name);
                        if (newOperand != null)
                            method.Body.Instructions[i] = Instruction.Create(OpCodes.Call,
                                _moduleDefinition.ImportReference(newOperand));
                    }

                    // Change TaskAwaiter`1 to ConfiguredTaskAwaiter`1
                    if (methodRef.DeclaringType.Resolve().FullName == "System.Runtime.CompilerServices.TaskAwaiter`1")
                    {
                        var newOperand =
                            _genericConfiguredTaskAwaiterType.Methods.FirstOrDefault(m => m.Name == methodRef.Name);
                        if (newOperand != null)
                        {
                            var genericArguments = ((GenericInstanceType)methodRef.DeclaringType).GenericArguments;

                            var newOperandRef = _moduleDefinition.ImportReference(newOperand);
                            newOperandRef.DeclaringType = _moduleDefinition
                                .ImportReference(_genericConfiguredTaskAwaiterType)
                                .MakeGenericInstanceType(genericArguments.ToArray());
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
                            if (awaitUnsafeOnCompleted.GenericArguments[j].FullName ==
                                "System.Runtime.CompilerServices.TaskAwaiter")
                                awaitUnsafeOnCompleted.GenericArguments[j] = _configuredTaskAwaiterTypeRef;
                            var theArg = awaitUnsafeOnCompleted.GenericArguments[j].Resolve();
                            if (theArg.FullName == "System.Runtime.CompilerServices.TaskAwaiter`1")
                            {
                                var genericArguments =
                                    ((GenericInstanceType)awaitUnsafeOnCompleted.GenericArguments[j]).GenericArguments;
                                awaitUnsafeOnCompleted.GenericArguments[j] = _moduleDefinition
                                    .ImportReference(_genericConfiguredTaskAwaiterType)
                                    .MakeGenericInstanceType(genericArguments.ToArray());
                            }
                        }
                    }
                }

                if (instruction.Operand is TypeReference typeRef)
                {
                    // Change TaskAwaiter to ConfiguredTaskAwaiter
                    if (typeRef.FullName == "System.Runtime.CompilerServices.TaskAwaiter")
                        instruction.Operand = _configuredTaskAwaiterTypeRef;
                    // Change TaskAwaiter`1 to ConfiguredTaskAwaiter`1
                    var theType = typeRef.Resolve();
                    if (theType?.FullName == "System.Runtime.CompilerServices.TaskAwaiter`1")
                    {
                        var genericArguments = ((GenericInstanceType)typeRef).GenericArguments;
                        instruction.Operand = _moduleDefinition.ImportReference(_genericConfiguredTaskAwaiterType)
                            .MakeGenericInstanceType(genericArguments.ToArray());
                    }
                }

                if (instruction.Operand is FieldReference fieldRef)
                {
                    // Change TaskAwaiter to ConfiguredTaskAwaiter
                    if (fieldRef.FieldType.FullName == "System.Runtime.CompilerServices.TaskAwaiter")
                        fieldRef.FieldType = _configuredTaskAwaiterTypeRef;
                    // Change TaskAwaiter`1 to ConfiguredTaskAwaiter`1
                    if (fieldRef.FieldType.IsGenericInstance)
                    {
                        var genericFieldType = (GenericInstanceType)fieldRef.FieldType;
                        var fieldtype = fieldRef.FieldType.Resolve();
                        if (fieldtype.FullName == "System.Runtime.CompilerServices.TaskAwaiter`1")
                        {
                            var genericArguments = genericFieldType.GenericArguments;
                            fieldRef.FieldType = _moduleDefinition.ImportReference(_genericConfiguredTaskAwaiterType)
                                .MakeGenericInstanceType(genericArguments.ToArray());
                        }
                    }
                }
            }

            method.Body.OptimizeMacros();
        }

        private static bool GetAwaiterSearch(Instruction instruction)
        {
            if (instruction.Operand is MethodReference method)
                return (method.DeclaringType.FullName == "System.Threading.Tasks.Task" ||
                        method.DeclaringType.Resolve().FullName == "System.Threading.Tasks.Task`1") &&
                       method.Name == "GetAwaiter";

            return false;
        }
    }
}