using System.Threading.Tasks;
using Fody;
using VerifyTests;
using VerifyXunit;
using Xunit;

public partial class ModuleWeaverTests
{
    [Fact]
    public Task DecompileExample()
    {
        var decompile = Ildasm.Decompile(testResult.AssemblyPath, "AssemblyToProcess.Example");
        var settings = new VerifySettings();
        settings.AutoVerify();
        return Verifier.Verify(decompile, settings);
    }

    [Fact]
    public Task DecompileIssue1()
    {
        var decompile = Ildasm.Decompile(testResult.AssemblyPath, "AssemblyToProcess.Issue1");
        var settings = new VerifySettings();
        settings.AutoVerify();
        return Verifier.Verify(decompile, settings);
    }

    [Fact]
    public Task DecompileGenericClass()
    {
        var decompile = Ildasm.Decompile(testResult.AssemblyPath, "AssemblyToProcess.GenericClass`1");
        var settings = new VerifySettings();
        settings.AutoVerify();
        return Verifier.Verify(decompile, settings);
    }

    [Fact]
    public Task DecompileGenericMethod()
    {
        var decompile = Ildasm.Decompile(testResult.AssemblyPath, "AssemblyToProcess.GenericMethod");
        var settings = new VerifySettings();
        settings.AutoVerify();
        return Verifier.Verify(decompile, settings);
    }

    [Fact]
    public Task DecompileCatchAndFinally()
    {
        var decompile = Ildasm.Decompile(testResult.AssemblyPath, "AssemblyToProcess.CatchAndFinally");
        return Verifier.Verify(decompile);
    }
}