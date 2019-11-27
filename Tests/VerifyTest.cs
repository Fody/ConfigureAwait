using System.Threading.Tasks;
using Fody;
using Xunit;

public partial class ModuleWeaverTests
{
    [Fact]
    public Task DecompileExample()
    {
        return Verify(Ildasm.Decompile(testResult.AssemblyPath, "AssemblyToProcess.Example"));
    }

    [Fact]
    public Task DecompileIssue1()
    {
        return Verify(Ildasm.Decompile(testResult.AssemblyPath, "AssemblyToProcess.Issue1"));
    }

    [Fact]
    public Task DecompileGenericClass()
    {
        return Verify(Ildasm.Decompile(testResult.AssemblyPath, "AssemblyToProcess.GenericClass`1"));
    }

    [Fact]
    public Task DecompileGenericMethod()
    {
        return Verify(Ildasm.Decompile(testResult.AssemblyPath, "AssemblyToProcess.GenericMethod"));
    }

    [Fact]
    public Task DecompileCatchAndFinally()
    {
        return Verify(Ildasm.Decompile(testResult.AssemblyPath, "AssemblyToProcess.CatchAndFinally"));
    }
}