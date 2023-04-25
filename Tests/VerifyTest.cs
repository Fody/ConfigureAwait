using Fody;
#pragma warning disable CS0618

public partial class ModuleWeaverTests
{
    [Fact]
    public Task DecompileExample()
    {
        var decompile = Ildasm.Decompile(testResult.AssemblyPath, "Example");
        var settings = new VerifySettings();
        settings.AutoVerify();
        return Verify(decompile, settings);
    }

    [Fact]
    public Task DecompileIssue1()
    {
        var decompile = Ildasm.Decompile(testResult.AssemblyPath, "Issue1");
        var settings = new VerifySettings();
        settings.AutoVerify();
        return Verify(decompile, settings);
    }

    [Fact]
    public Task DecompileGenericClass()
    {
        var decompile = Ildasm.Decompile(testResult.AssemblyPath, "GenericClass`1");
        var settings = new VerifySettings();
        settings.AutoVerify();
        return Verify(decompile, settings);
    }

    [Fact]
    public Task DecompileGenericMethod()
    {
        var decompile = Ildasm.Decompile(testResult.AssemblyPath, "GenericMethod");
        var settings = new VerifySettings();
        settings.AutoVerify();
        return Verify(decompile, settings);
    }

    [Fact]
    public Task DecompileCatchAndFinally()
    {
        var decompile = Ildasm.Decompile(testResult.AssemblyPath, "CatchAndFinally");
        var settings = new VerifySettings();
        settings.AutoVerify();
        return Verify(decompile, settings);
    }
}