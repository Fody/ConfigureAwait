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

            var configureAwaitMethodDef = typeFinder.GetMSCorLibTypeDefinition("System.Threading.Tasks.Task").Methods.First(m => m.Name == "ConfigureAwait");
            var configureAwaitMethodRef = ModuleDefinition.Import(configureAwaitMethodDef);

            foreach (var field in type.Fields)
            {
                // Change field type
                if (field.FieldType.FullName == "System.Runtime.CompilerServices.TaskAwaiter")
                    field.FieldType = configuredTaskAwaiterTypeRef;
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
            }

            // Define Awaitable temp variable
            var configAwaitableVar = new Lazy<VariableDefinition>(() =>
            {
                var variable = new VariableDefinition(configuredTaskAwaitableTypeRef);
                method.Body.Variables.Insert(2, variable);
                return variable;
            });

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

                    // Change TaskAwaiter to ConfiguredTaskAwaiter
                    if (methodRef.DeclaringType.FullName == "System.Runtime.CompilerServices.TaskAwaiter")
                    {
                        var newOperand = configuredTaskAwaiterTypeDef.Methods.FirstOrDefault(m => m.Name == methodRef.Name);
                        if (newOperand != null)
                        {
                            method.Body.Instructions[i] = Instruction.Create(OpCodes.Call, ModuleDefinition.Import(newOperand));
                        }
                    }

                    // Change AwaitUnsafeOnCompleted<TaskAwaiter, T> to AwaitUnsafeOnCompleted<ConfiguredTaskAwaiter, T>
                    if (methodRef.IsGenericInstance && methodRef.Name == "AwaitUnsafeOnCompleted")
                    {
                        var awaitUnsafeOnCompleted = (GenericInstanceMethod)methodRef;

                        for (int j = 0; j < awaitUnsafeOnCompleted.GenericArguments.Count; j++)
                        {
                            if (awaitUnsafeOnCompleted.GenericArguments[j].FullName == "System.Runtime.CompilerServices.TaskAwaiter")
                            {
                                awaitUnsafeOnCompleted.GenericArguments[j] = configuredTaskAwaiterTypeRef;
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
                }
            }

            method.Body.OptimizeMacros();
        }

        private bool GetAwaiterSearch(Instruction instruction)
        {
            var method = instruction.Operand as MethodReference;

            if (method != null)
                return method.DeclaringType.FullName == "System.Threading.Tasks.Task" && method.Name == "GetAwaiter";

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