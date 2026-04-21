using Fody;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

public partial class ModuleWeaver
{
    /// <summary>
    /// Rewrites calls to <c>System.Runtime.CompilerServices.AsyncHelpers.Await(task)</c> inside
    /// <paramref name="method"/> so that each awaited task is first configured via <c>ConfigureAwait(<paramref name="configureAwaitValue"/>)</c>.
    /// </summary>
    /// <remarks>
    /// The rewrite handles all four awaitable variants — <c>Task</c>, <c>Task&lt;T&gt;</c>, <c>ValueTask</c> and <c>ValueTask&lt;T&gt;</c> — and retargets the original
    /// <c>Await</c> call to the overload that accepts the corresponding configured-awaitable type.
    /// For value-type awaitables a temporary local is introduced so that <c>ConfigureAwait</c> can be called as an instance method via a managed pointer (<c>ldloca</c> / <c>call</c>).
    /// </remarks>
    /// <param name="method">The method whose IL body is to be rewritten.</param>
    /// <param name="configureAwaitValue">
    /// The value passed to <c>ConfigureAwait</c>: <see langword="true"/> to continue on the
    /// captured synchronisation context; <see langword="false"/> to suppress it.
    /// </param>
    void AddAwaitConfigToAsyncMethod(MethodDefinition method, bool configureAwaitValue)
    {
        var body = method.Body;

        body.SimplifyMacros();

        var ilProcessor = body.GetILProcessor();

        foreach (var instruction in body.Instructions.ToList())
        {
            // Find calls to AsyncHelpers.Await, which is the method we inject for awaiting tasks. We rewrite these calls to first call ConfigureAwait on the task, then pass the resulting configured awaitable to an overload of Await that accepts it.

            if (instruction.OpCode != OpCodes.Call)
                continue;

            var methodReference = (MethodReference)instruction.Operand;

            if (methodReference.Name != "Await" || methodReference.DeclaringType.FullName != "System.Runtime.CompilerServices.AsyncHelpers")
                continue;

            var methodDefinition = methodReference.Resolve() ?? throw new WeavingException($"Failed to resolve method reference: {methodReference.FullName}");
            if (!methodDefinition.IsStatic)
                continue;

            if (methodReference.Parameters.Count != 1)
                continue;

            var parameterTypeReference = methodReference.Parameters[0].ParameterType;

            // Skip if the parameter has already a configured awaitable. In that case the configureAwait call has already been applied by source code, so we can skip it.
            if (parameterTypeReference.Name.StartsWith("Configured", StringComparison.Ordinal))
                continue;

            var declaringType = methodDefinition.DeclaringType;

            MethodReference configureAwaitMethodRef;
            MethodReference targetMethodRef;
            bool isValueType;

            var paramTypeDefinition = parameterTypeReference.Resolve() ?? throw new WeavingException($"Failed to resolve parameter type: {parameterTypeReference.FullName}");
            string configuredAwaitableName;

            // Determine the appropriate ConfigureAwait method to call based on the parameter type, and the corresponding await-overload to retarget to.
            if (methodDefinition.HasGenericParameters)
            {
                if (methodReference is not GenericInstanceMethod genericCallRef)
                    continue;

                var typeArg = genericCallRef.GenericArguments[0];

                TypeReference taskBaseType;
                MethodDefinition configureAwaitMethodDef;

                switch (paramTypeDefinition.Name)
                {
                    case "ValueTask`1":
                        taskBaseType = genericValueTaskType;
                        configureAwaitMethodDef = genericValueTaskConfigureAwaitMethodDef;
                        configuredAwaitableName = "ConfiguredValueTaskAwaitable`1";
                        isValueType = true;
                        break;
                    
                    case "Task`1":
                        taskBaseType = genericTaskType;
                        configureAwaitMethodDef = genericTaskConfigureAwaitMethodDef;
                        configuredAwaitableName = "ConfiguredTaskAwaitable`1";
                        isValueType = false;
                        break;
                    
                    default:
                        continue;
                }

                var taskT = taskBaseType.MakeGenericInstanceType(typeArg);
                configureAwaitMethodRef = ModuleDefinition.ImportReference(configureAwaitMethodDef);
                configureAwaitMethodRef.DeclaringType = taskT;

                var targetMethodDefinition = FindAwaitMethodDefinition(declaringType, configuredAwaitableName); 

                var genericTargetMethodRef = new GenericInstanceMethod(ModuleDefinition.ImportReference(targetMethodDefinition));
                genericTargetMethodRef.GenericArguments.Add(typeArg);
                targetMethodRef = genericTargetMethodRef;
            }
            else
            {
                switch (paramTypeDefinition.Name)
                {
                    case "ValueTask":
                        configureAwaitMethodRef = valueTaskConfigureAwaitMethod;
                        configuredAwaitableName = "ConfiguredValueTaskAwaitable";
                        isValueType = true;
                        break;

                    case "Task":
                        configureAwaitMethodRef = taskConfigureAwaitMethod;
                        configuredAwaitableName = "ConfiguredTaskAwaitable";
                        isValueType = false;
                        break;

                    default:
                        continue;
                }

                var targetMethodDefinition = FindAwaitMethodDefinition(declaringType, configuredAwaitableName);

                targetMethodRef = ModuleDefinition.ImportReference(targetMethodDefinition);
            }

            // Inline ConfigureAwait directly before the Await call, then retarget the call to Await(Configured*Awaitable).
            // For reference-type awaitables (Task, Task<T>): the value is already a reference on the stack, so callvirt works directly.
            // For value-type awaitables (ValueTask, ValueTask<T>): ConfigureAwait is an instance method on a struct, so its 'this' parameter must be a managed pointer. We stash the value into a new local and ldloca it, then use call (not callvirt).
            var configureAwaitParameterInstruction = Instruction.Create(OpCodes.Ldc_I4, configureAwaitValue ? 1 : 0);

            if (isValueType)
            {
                // Determine the concrete value-type to use for the temp local.
                TypeReference localType;
                if (methodDefinition.HasGenericParameters && methodReference is GenericInstanceMethod)
                {
                    // e.g. ValueTask`1<int>
                    localType = (GenericInstanceType)configureAwaitMethodRef.DeclaringType;
                }
                else
                {
                    localType = ModuleDefinition.ImportReference(parameterTypeReference.Resolve() ?? throw new WeavingException($"Failed to resolve parameter type: {parameterTypeReference.FullName}"));
                }

                var tempLocal = new VariableDefinition(localType);
                body.Variables.Add(tempLocal);

                // stloc  <tempLocal>          ; save the ValueTask from the stack
                // ldloca <tempLocal>          ; push managed pointer to it
                // ldc.i4 <configureAwaitArg>
                // call   ValueTask::ConfigureAwait(bool)
                ilProcessor.InsertBefore(instruction, Instruction.Create(OpCodes.Stloc, tempLocal));
                ilProcessor.InsertBefore(instruction, Instruction.Create(OpCodes.Ldloca, tempLocal));
                ilProcessor.InsertBefore(instruction, configureAwaitParameterInstruction);
                ilProcessor.InsertBefore(instruction, Instruction.Create(OpCodes.Call, configureAwaitMethodRef));
            }
            else
            {
                ilProcessor.InsertBefore(instruction, configureAwaitParameterInstruction);
                ilProcessor.InsertBefore(instruction, Instruction.Create(OpCodes.Callvirt, configureAwaitMethodRef));
            }

            instruction.Operand = targetMethodRef;
        }

        body.OptimizeMacros();
    }

    static MethodDefinition FindAwaitMethodDefinition(TypeDefinition declaringType, string configuredAwaitableName)
    {
        return declaringType.Methods.FirstOrDefault(m =>
            m.Name == "Await" &&
            m.Parameters.Count == 1 &&
            m.Parameters[0].ParameterType.Name == configuredAwaitableName) ?? throw new WeavingException($"Failed to find target method: Await({configuredAwaitableName})");
    }
}
