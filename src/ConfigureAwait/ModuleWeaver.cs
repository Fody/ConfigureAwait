using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace ConfigureAwait
{
    public class ModuleWeaver
    {
        public Action<string> LogInfo { get; set; }

        public Action<string> LogWarning { get; set; }

        public Action<string> LogError { get; set; }

        public ModuleDefinition ModuleDefinition { get; set; }

        public IAssemblyResolver AssemblyResolver { get; set; }

        public string[] DefineConstants { get; set; }

        private TypeFinder typeFinder;

        public void Execute()
        {
            LoggerFactory.LogInfo = LogInfo;
            LoggerFactory.LogWarn = LogWarning;
            LoggerFactory.LogError = LogError;

            typeFinder = new TypeFinder(AssemblyResolver, ModuleDefinition);

            var types = ModuleDefinition.GetTypes()
                .ToList();

            foreach (var type in types)
            {
                ProcessType(type);
            }

            RemoveAttributes(types);
            RemoveReference();
        }

        private void ProcessType(TypeDefinition type)
        {
            if (type.IsCompilerGenerated() && type.IsIAsyncStateMachine())
            {
                return;
            }

            var configureAwaitValue = (bool?)type.GetConfigureAwaitAttribute()?.ConstructorArguments[0].Value;

            foreach (var method in type.Methods)
            {
                var localConfigureAwaitValue = (bool?)method.GetConfigureAwaitAttribute()?.ConstructorArguments[0].Value;
                var localConfigWasSet = localConfigureAwaitValue.HasValue;
                localConfigureAwaitValue = localConfigureAwaitValue ?? configureAwaitValue;
                if (localConfigureAwaitValue == null)
                    continue;

                var asyncStateMachineType = method.GetAsyncStateMachineType();
                if (asyncStateMachineType != null)
                {
                    AddAwaitConfigToAsyncMethod(asyncStateMachineType, localConfigureAwaitValue.Value);
                }
                else if (localConfigWasSet)
                {
                    LogWarning("ConfigureAwaitAttribue applied to non-async method '\{method.FullName}'.");
                    continue;
                }
            }
        }

        private void AddAwaitConfigToAsyncMethod(TypeDefinition type, bool value)
        {
            var configuredTaskAwaitableTypeDef = typeFinder.GetMSCorLibTypeDefinition("System.Runtime.CompilerServices.ConfiguredTaskAwaitable");
            var configuredTaskAwaiterTypeDef = configuredTaskAwaitableTypeDef.NestedTypes[0];
            var configuredTaskAwaitableTypeRef = ModuleDefinition.Import(configuredTaskAwaitableTypeDef);
            var configuredTaskAwaiterTypeRef = ModuleDefinition.Import(configuredTaskAwaiterTypeDef);

            var gConfiguredTaskAwaitableTypeDef = typeFinder.GetMSCorLibTypeDefinition("System.Runtime.CompilerServices.ConfiguredTaskAwaitable`1");
            var gConfiguredTaskAwaiterTypeDef = gConfiguredTaskAwaitableTypeDef.NestedTypes[0];

            IEnumerable<TypeReference> genericArguments = null;

            foreach (var field in type.Fields)
            {
                // Change field type
                if (field.FieldType.FullName == "System.Runtime.CompilerServices.TaskAwaiter")
                    field.FieldType = configuredTaskAwaiterTypeRef;
                if (field.FieldType.IsGenericInstance)
                {
                    var genericFieldType = (GenericInstanceType)field.FieldType;
                    var fieldtype = field.FieldType.Resolve();
                    if (fieldtype.FullName == "System.Runtime.CompilerServices.TaskAwaiter`1")
                    {
                        genericArguments = genericFieldType.GenericArguments;
                        field.FieldType = ModuleDefinition.Import(gConfiguredTaskAwaiterTypeDef.MakeGenericInstanceType(genericArguments));
                    }
                }
            }

            // Modify MoveNext method
            var method = type.Methods.First(m => m.Name == "MoveNext");
            method.Body.SimplifyMacros();
            var ilProcessor = method.Body.GetILProcessor();

            foreach (var v in method.Body.Variables)
            {
                // Change variable type
                if (v.VariableType.FullName == "System.Runtime.CompilerServices.TaskAwaiter")
                    v.VariableType = configuredTaskAwaiterTypeRef;
                if (v.VariableType.IsGenericInstance)
                {
                    var genericVariableType = (GenericInstanceType)v.VariableType;
                    var variableType = v.VariableType.Resolve();
                    if (variableType.FullName == "System.Runtime.CompilerServices.TaskAwaiter`1")
                        v.VariableType = ModuleDefinition.Import(gConfiguredTaskAwaiterTypeDef.MakeGenericInstanceType(genericVariableType.GenericArguments));
                }
            }

            // Define Awaitable temp variable
            var configAwaitableVar = new Lazy<VariableDefinition>(() =>
            {
                var variable = genericArguments == null ?
                    new VariableDefinition(configuredTaskAwaitableTypeRef) :
                    new VariableDefinition(ModuleDefinition.Import(gConfiguredTaskAwaitableTypeDef.MakeGenericInstanceType(genericArguments)));
                method.Body.Variables.Add(variable);
                return variable;
            });

            // Find the ConfigureAwait method
            var configureAwaitMethodDef = genericArguments == null ?
                typeFinder.GetMSCorLibTypeDefinition("System.Threading.Tasks.Task").Methods.First(m => m.Name == "ConfigureAwait") :
                typeFinder.GetMSCorLibTypeDefinition("System.Threading.Tasks.Task`1").Methods.First(m => m.Name == "ConfigureAwait");
            var configureAwaitMethodRef = ModuleDefinition.Import(configureAwaitMethodDef);
            if (genericArguments != null)
            {
                configureAwaitMethodRef.DeclaringType = ModuleDefinition.Import(typeFinder.GetMSCorLibTypeDefinition("System.Threading.Tasks.Task`1").MakeGenericInstanceType(genericArguments));
            }

            // Insert ConfigureAwait call just before GetAwaiter call.
            foreach (var instruction in method.Body.Instructions.Where(GetAwaiterSearch).ToList())
            {
                ilProcessor.InsertBefore(instruction,
                    Instruction.Create(value ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0), // true or false
                    Instruction.Create(OpCodes.Callvirt, configureAwaitMethodRef), // Call ConfigureAwait
                    Instruction.Create(OpCodes.Stloc, configAwaitableVar.Value), // Store in variable
                    Instruction.Create(OpCodes.Ldloca, configAwaitableVar.Value) // Load variable
                );
            }

            for (int i = 0; i < method.Body.Instructions.Count; i++)
            {
                var instruction = method.Body.Instructions[i];

                var methodRef = instruction.Operand as MethodReference;
                if (methodRef != null)
                {
                    // Change Task to ConfiguredTaskAwaitable
                    if (methodRef.DeclaringType.FullName == "System.Threading.Tasks.Task")
                    {
                        var newOperand = configuredTaskAwaitableTypeDef.Methods.FirstOrDefault(m => m.Name == methodRef.Name);
                        if (newOperand != null)
                        {
                            method.Body.Instructions[i] = Instruction.Create(OpCodes.Call, ModuleDefinition.Import(newOperand));
                        }
                    }

                    // Change Task`1 to ConfiguredTaskAwaitable`1
                    if (methodRef.DeclaringType.Resolve().FullName == "System.Threading.Tasks.Task`1")
                    {
                        var newOperand = gConfiguredTaskAwaitableTypeDef.Methods.FirstOrDefault(m => m.Name == methodRef.Name);
                        if (newOperand != null)
                        {
                            var newOperandRef = ModuleDefinition.Import(newOperand);
                            newOperandRef.DeclaringType = ModuleDefinition.Import(gConfiguredTaskAwaitableTypeDef.MakeGenericInstanceType(genericArguments));
                            method.Body.Instructions[i] = Instruction.Create(OpCodes.Call, newOperandRef);
                        }
                    }

                    // Change TaskAwaiter to ConfiguredTaskAwaiter
                    if (methodRef.DeclaringType.FullName == "System.Runtime.CompilerServices.TaskAwaiter")
                    {
                        var newOperand = configuredTaskAwaiterTypeDef.Methods.FirstOrDefault(m => m.Name == methodRef.Name);
                        if (newOperand != null)
                        {
                            method.Body.Instructions[i] = Instruction.Create(OpCodes.Call, ModuleDefinition.Import(newOperand));
                        }
                    }

                    // Change TaskAwaiter`1 to ConfiguredTaskAwaiter`1
                    if (methodRef.DeclaringType.Resolve().FullName == "System.Runtime.CompilerServices.TaskAwaiter`1")
                    {
                        var newOperand = gConfiguredTaskAwaiterTypeDef.Methods.FirstOrDefault(m => m.Name == methodRef.Name);
                        if (newOperand != null)
                        {
                            var newOperandRef = ModuleDefinition.Import(newOperand);
                            newOperandRef.DeclaringType = ModuleDefinition.Import(gConfiguredTaskAwaiterTypeDef.MakeGenericInstanceType(genericArguments));
                            method.Body.Instructions[i] = Instruction.Create(OpCodes.Call, newOperandRef);
                        }
                    }

                    // Change AwaitUnsafeOnCompleted<TaskAwaiter, T> to AwaitUnsafeOnCompleted<ConfiguredTaskAwaiter, T>
                    // Change AwaitUnsafeOnCompleted<TaskAwaiter`1, T> to AwaitUnsafeOnCompleted<ConfiguredTaskAwaiter`1, T>
                    if (methodRef.IsGenericInstance && methodRef.Name == "AwaitUnsafeOnCompleted")
                    {
                        var awaitUnsafeOnCompleted = (GenericInstanceMethod)methodRef;

                        for (int j = 0; j < awaitUnsafeOnCompleted.GenericArguments.Count; j++)
                        {
                            if (awaitUnsafeOnCompleted.GenericArguments[j].FullName == "System.Runtime.CompilerServices.TaskAwaiter")
                            {
                                awaitUnsafeOnCompleted.GenericArguments[j] = configuredTaskAwaiterTypeRef;
                            }
                            if (awaitUnsafeOnCompleted.GenericArguments[j].Resolve().FullName == "System.Runtime.CompilerServices.TaskAwaiter`1")
                            {
                                awaitUnsafeOnCompleted.GenericArguments[j] = ModuleDefinition.Import(gConfiguredTaskAwaiterTypeDef.MakeGenericInstanceType(genericArguments));
                            }
                        }
                    }
                }

                var typeRef = instruction.Operand as TypeReference;
                if (typeRef != null)
                {
                    // Change TaskAwaiter to ConfiguredTaskAwaiter
                    if (typeRef.FullName == "System.Runtime.CompilerServices.TaskAwaiter")
                    {
                        instruction.Operand = configuredTaskAwaiterTypeRef;
                    }

                    // Change TaskAwaiter`1 to ConfiguredTaskAwaiter`1
                    if (typeRef.Resolve().FullName == "System.Runtime.CompilerServices.TaskAwaiter`1")
                    {
                        instruction.Operand = ModuleDefinition.Import(gConfiguredTaskAwaiterTypeDef.MakeGenericInstanceType(genericArguments));
                    }
                }
            }

            method.Body.OptimizeMacros();
        }

        private Instruction ReplaceMethodCall(MethodReference methodRef, string typeName, MethodDefinition newOperand, OpCode opCode)
        {
            if (methodRef.DeclaringType.FullName == typeName)
            {
                if (newOperand != null)
                {
                    return Instruction.Create(opCode, ModuleDefinition.Import(newOperand));
                }
            }

            return null;
        }

        private bool GetAwaiterSearch(Instruction instruction)
        {
            var method = instruction.Operand as MethodReference;

            if (method != null)
                return (method.DeclaringType.FullName == "System.Threading.Tasks.Task" || method.DeclaringType.Resolve().FullName == "System.Threading.Tasks.Task`1")
                    && method.Name == "GetAwaiter";

            return false;
        }

        private void RemoveAttributes(List<TypeDefinition> types)
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

        private void RemoveReference()
        {
            var referenceToRemove = ModuleDefinition.AssemblyReferences.FirstOrDefault(x => x.Name == "ConfigureAwait");
            if (referenceToRemove == null)
            {
                LogInfo("\tNo reference to 'ConfigureAwait.dll' found. References not modified.");
                return;
            }

            ModuleDefinition.AssemblyReferences.Remove(referenceToRemove);
            LogInfo("\tRemoving reference to 'ConfigureAwait.dll'.");
        }
    }
}