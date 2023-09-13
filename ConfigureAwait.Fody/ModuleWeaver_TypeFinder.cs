using Mono.Cecil;

public partial class ModuleWeaver
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

    void FindTypes()
    {
        taskDef = FindTypeDefinition("System.Threading.Tasks.Task");
        var configureTaskAwaitMethodDef = taskDef.Methods.First(_ => _.Name == "ConfigureAwait");
        taskConfigureAwaitMethod = ModuleDefinition.ImportReference(configureTaskAwaitMethodDef);
        configuredTaskAwaitableTypeDef = FindTypeDefinition("System.Runtime.CompilerServices.ConfiguredTaskAwaitable");
        configuredTaskAwaiterTypeDef = configuredTaskAwaitableTypeDef.NestedTypes[0];
        configuredTaskAwaitableTypeRef = ModuleDefinition.ImportReference(configuredTaskAwaitableTypeDef);
        configuredTaskAwaiterTypeRef = ModuleDefinition.ImportReference(configuredTaskAwaiterTypeDef);

        var genericTaskDef = FindTypeDefinition("System.Threading.Tasks.Task`1");
        genericTaskConfigureAwaitMethodDef = genericTaskDef.Methods.First(_ => _.Name == "ConfigureAwait");
        genericConfiguredTaskAwaitableTypeDef = FindTypeDefinition("System.Runtime.CompilerServices.ConfiguredTaskAwaitable`1");
        genericConfiguredTaskAwaiterTypeDef = genericConfiguredTaskAwaitableTypeDef.NestedTypes[0];
        genericConfiguredTaskAwaiterTypeRef = ModuleDefinition.ImportReference(genericConfiguredTaskAwaiterTypeDef);
        genericConfiguredTaskAwaitableTypeRef = ModuleDefinition.ImportReference(genericConfiguredTaskAwaitableTypeDef);
        genericTaskType = ModuleDefinition.ImportReference(genericTaskDef);

        if (TryFindTypeDefinition("System.Threading.Tasks.ValueTask", out valueTaskDef))
        {
            var configureValueTaskAwaitMethodDef = valueTaskDef.Methods.First(_ => _.Name == "ConfigureAwait");
            valueTaskConfigureAwaitMethod = ModuleDefinition.ImportReference(configureValueTaskAwaitMethodDef);
            configuredValueTaskAwaitableTypeDef = FindTypeDefinition("System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable");
            configuredValueTaskAwaiterTypeDef = configuredValueTaskAwaitableTypeDef.NestedTypes[0];
            configuredValueTaskAwaitableTypeRef = ModuleDefinition.ImportReference(configuredValueTaskAwaitableTypeDef);
            configuredValueTaskAwaiterTypeRef = ModuleDefinition.ImportReference(configuredValueTaskAwaiterTypeDef);
        }

        if (TryFindTypeDefinition("System.Threading.Tasks.ValueTask`1", out var genericValueTaskDef))
        {
            genericValueTaskConfigureAwaitMethodDef = genericValueTaskDef.Methods.First(_ => _.Name == "ConfigureAwait");
            genericConfiguredValueTaskAwaitableTypeDef = FindTypeDefinition("System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable`1");
            genericConfiguredValueTaskAwaiterTypeDef = genericConfiguredValueTaskAwaitableTypeDef.NestedTypes[0];
            genericConfiguredValueTaskAwaiterTypeRef = ModuleDefinition.ImportReference(genericConfiguredValueTaskAwaiterTypeDef);
            genericConfiguredValueTaskAwaitableTypeRef = ModuleDefinition.ImportReference(genericConfiguredValueTaskAwaitableTypeDef);
            genericValueTaskType = ModuleDefinition.ImportReference(genericValueTaskDef);
        }
    }
}