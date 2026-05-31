using Mono.Cecil;
using Mono.Cecil.Cil;

public partial class ModuleWeaverTests
{
    // Awaiting a ternary directly (await (cond ? taskA : taskB)) makes the
    // compiler emit one branch that jumps straight to the merged GetAwaiter.
    // The weaver inserts the ConfigureAwait call just before that GetAwaiter, so
    // the jumping branch bypasses it and reaches ConfiguredTaskAwaitable.GetAwaiter
    // with a raw Task on the stack. That corrupt IL is what throws a
    // NullReferenceException in Task.AddTaskContinuationComplex at runtime.
    // https://github.com/Fody/ConfigureAwait/issues/537
    [Fact]
    public void AwaitTernary_BothBranchesAreConfigured()
    {
        using var module = ModuleDefinition.ReadModule(testResult.AssemblyPath);

        var stateMachine = module
            .GetType("AwaitTernary")
            .NestedTypes
            .Single(_ => _.Methods.Any(method => method.Name == "MoveNext"));
        var moveNext = stateMachine.Methods.Single(_ => _.Name == "MoveNext");

        var instructions = moveNext.Body.Instructions;

        var getAwaiterCalls = instructions
            .Where(_ => _.OpCode.FlowControl == FlowControl.Call &&
                        _.Operand is MethodReference { Name: "GetAwaiter" })
            .ToList();

        // Every path reaching a GetAwaiter must first flow through the inserted
        // ConfigureAwait. A branch that targets the GetAwaiter directly skips it,
        // so no branch instruction may point at a GetAwaiter call.
        var branchesSkippingConfigureAwait = instructions
            .Where(_ => _.Operand is Instruction target && getAwaiterCalls.Contains(target))
            .ToList();

        Assert.Empty(branchesSkippingConfigureAwait);
    }
}
