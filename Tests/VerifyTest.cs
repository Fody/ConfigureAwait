using ApprovalTests;
using Fody;
using Xunit;

public partial class ModuleWeaverTests
{
    [Fact]
    public void DecompileExample()
    {
        Approvals.Verify(Ildasm.Decompile(testResult.AssemblyPath, "AssemblyToProcess.Example"));
    }

    [Fact]
    public void DecompileIssue1()
    {
        Approvals.Verify(Ildasm.Decompile(testResult.AssemblyPath, "AssemblyToProcess.Issue1"));
    }

    [Fact]
    public void DecompileGenericClass()
    {
        Approvals.Verify(Ildasm.Decompile(testResult.AssemblyPath, "AssemblyToProcess.GenericClass`1"));
    }

    [Fact]
    public void DecompileGenericMethod()
    {
        Approvals.Verify(Ildasm.Decompile(testResult.AssemblyPath, "AssemblyToProcess.GenericMethod"));
    }

    [Fact]
    public void DecompileCatchAndFinally()
    {
        Approvals.Verify(Ildasm.Decompile(testResult.AssemblyPath, "AssemblyToProcess.CatchAndFinally"));
    }
}