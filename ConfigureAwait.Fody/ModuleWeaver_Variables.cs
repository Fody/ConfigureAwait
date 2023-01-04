using Mono.Cecil;
using Mono.Cecil.Cil;

public partial class ModuleWeaver
{
    void ProcessVariables(bool configureAwaitValue, MethodDefinition method)
    {
        var awaitAwaiterPair = new Dictionary<VariableDefinition, VariableDefinition>();
        var configureAwaitMethods = new Dictionary<VariableDefinition, MethodReference>();

        var variableIndex = 0;

        var body = method.Body;
        var variables = body.Variables;
        foreach (var variable in variables.ToArray())
        {
            if (IsAwaiterVariable(variable, out var awaitableVar, out var localConfigAwait))
            {
                variables.Insert(variableIndex + 1, awaitableVar);
                awaitAwaiterPair.Add(variable, awaitableVar);
                configureAwaitMethods.Add(variable, localConfigAwait);
            }

            variableIndex++;
        }

        var ilProcessor = body.GetILProcessor();
        // Insert ConfigureAwait call just before GetAwaiter call.
        foreach (var instruction in body.Instructions.Where(GetAwaiterSearch).ToList())
        {
            var variable = (VariableDefinition)instruction.Next.Operand;
            var awaitableVar = awaitAwaiterPair[variable];
            var configureAwait = configureAwaitMethods[variable];

            ilProcessor.InsertBefore(instruction,
                // true or false
                Instruction.Create(configureAwaitValue ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0),
                // Call ConfigureAwait
                Instruction.Create(OpCodes.Callvirt, configureAwait),
                // Store in variable
                Instruction.Create(OpCodes.Stloc, awaitableVar),
                // Load variable
                Instruction.Create(OpCodes.Ldloca, awaitableVar)
            );
        }
    }

    bool IsAwaiterVariable(VariableDefinition variable, out VariableDefinition awaitableVar, out MethodReference localConfigAwait)
    {
        // Change variable type
        if (variable.VariableType.FullName == "System.Runtime.CompilerServices.TaskAwaiter")
        {
            variable.VariableType = configuredTaskAwaiterTypeRef;
            awaitableVar = new(configuredTaskAwaitableTypeRef);
            localConfigAwait = taskConfigureAwaitMethod;
            return true;
        }

        if (variable.VariableType.FullName == "System.Runtime.CompilerServices.ValueTaskAwaiter")
        {
            variable.VariableType = configuredValueTaskAwaiterTypeRef;
            awaitableVar = new(configuredValueTaskAwaitableTypeRef);
            localConfigAwait = valueTaskConfigureAwaitMethod;
            return true;
        }

        if (variable.VariableType.IsGenericInstance)
        {
            var genericVariableType = (GenericInstanceType)variable.VariableType;
            var variableType = variable.VariableType.Resolve();

            if (variableType.FullName == "System.Runtime.CompilerServices.TaskAwaiter`1")
            {
                variable.VariableType = genericConfiguredTaskAwaiterTypeRef.MakeGenericInstanceType(genericVariableType.GenericArguments);
                awaitableVar = new(genericConfiguredTaskAwaitableTypeRef.MakeGenericInstanceType(genericVariableType.GenericArguments));
                localConfigAwait = ModuleDefinition.ImportReference(genericTaskConfigureAwaitMethodDef);
                localConfigAwait.DeclaringType = genericTaskType.MakeGenericInstanceType(genericVariableType.GenericArguments);
                return true;
            }

            if (variableType.FullName == "System.Runtime.CompilerServices.ValueTaskAwaiter`1")
            {
                variable.VariableType = genericConfiguredValueTaskAwaiterTypeRef.MakeGenericInstanceType(genericVariableType.GenericArguments);
                awaitableVar = new(genericConfiguredValueTaskAwaitableTypeRef.MakeGenericInstanceType(genericVariableType.GenericArguments));
                localConfigAwait = ModuleDefinition.ImportReference(genericValueTaskConfigureAwaitMethodDef);
                localConfigAwait.DeclaringType = genericValueTaskType.MakeGenericInstanceType(genericVariableType.GenericArguments);
                return true;
            }
        }

        awaitableVar = null;
        localConfigAwait = null;
        return false;
    }

    static bool GetAwaiterSearch(Instruction instruction)
    {
        if (instruction.Operand is not MethodReference method)
        {
            return false;
        }

        if (method.Name != "GetAwaiter")
        {
            return false;
        }

        var declaring = method.DeclaringType;

        if (declaring.FullName is
            "System.Threading.Tasks.Task" or "System.Threading.Tasks.ValueTask")
        {
            return true;
        }

        return declaring.Resolve().FullName is
            "System.Threading.Tasks.Task`1" or "System.Threading.Tasks.ValueTask`1";
    }
}