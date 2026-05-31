using Mono.Cecil;
using Mono.Cecil.Cil;

public partial class ModuleWeaverTests
{
    // Awaiting a ternary directly (await (cond ? taskA : taskB)) makes the
    // compiler emit one branch that jumps straight to the point where the task is
    // awaited (the GetAwaiter call for a compiler state machine, or the
    // AsyncHelpers.Await call for runtime-async). The weaver inserts the
    // ConfigureAwait call just before that point, so the jumping branch bypasses
    // it and awaits a raw, unconfigured task. That corrupt IL is what throws a
    // NullReferenceException in Task.AddTaskContinuationComplex at runtime.
    // https://github.com/Fody/ConfigureAwait/issues/537
    [Fact]
    public void AwaitTernary_BothBranchesAreConfigured()
    {
        using var module = ModuleDefinition.ReadModule(testResult.AssemblyPath);

        var type = module.GetType("AwaitTernary");
        var methods = type.Methods
            .Concat(type.NestedTypes.SelectMany(_ => _.Methods))
            .Where(_ => _.HasBody);

        var awaitSinks = new List<Instruction>();
        var branchesSkippingConfigureAwait = new List<Instruction>();

        foreach (var method in methods)
        {
            var instructions = method.Body.Instructions;

            var sinks = instructions.Where(IsAwaitSink).ToList();
            awaitSinks.AddRange(sinks);

            // Every path reaching the await must first flow through the inserted
            // ConfigureAwait. A branch that targets the await directly skips it.
            branchesSkippingConfigureAwait.AddRange(
                instructions.Where(_ =>
                    _.Operand is Instruction target && sinks.Contains(target) ||
                    _.Operand is Instruction[] targets && targets.Any(sinks.Contains)));
        }

        Assert.NotEmpty(awaitSinks);
        Assert.Empty(branchesSkippingConfigureAwait);
    }

    static bool IsAwaitSink(Instruction instruction) =>
        instruction.Operand is MethodReference method &&
        (method.Name == "GetAwaiter" ||
         method is { Name: "Await", DeclaringType.FullName: "System.Runtime.CompilerServices.AsyncHelpers" });
}
