using Fody;
#pragma warning disable CS0618

public partial class ModuleWeaverTests
{
    [Fact]
    public Task DecompileExample()
    {
        var decompile = Ildasm.Decompile(testResult.AssemblyPath, "Example");
        return Verify(decompile, GetSettings());
    }

    [Fact]
    public Task DecompileIssue1()
    {
        var decompile = Ildasm.Decompile(testResult.AssemblyPath, "Issue1");
        return Verify(decompile, GetSettings());
    }

    [Fact]
    public Task DecompileGenericClass()
    {
        var decompile = Ildasm.Decompile(testResult.AssemblyPath, "GenericClass`1");
        return Verify(decompile, GetSettings());
    }

    [Fact]
    public Task DecompileGenericMethod()
    {
        var decompile = Ildasm.Decompile(testResult.AssemblyPath, "GenericMethod");
        return Verify(decompile, GetSettings());
    }

    [Fact]
    public Task DecompileCatchAndFinally()
    {
        var decompile = Ildasm.Decompile(testResult.AssemblyPath, "CatchAndFinally");
        return Verify(decompile, GetSettings());
    }

    [Fact]
    public Task DecompileAwaitTernary()
    {
        var decompile = Ildasm.Decompile(testResult.AssemblyPath, "AwaitTernary");
        return Verify(decompile, GetSettings());
    }

#if NET
    [Fact]
    public Task DecompileAsyncEnumerable()
    {
        var decompile = Ildasm.Decompile(testResult.AssemblyPath, "AsyncEnumerable");
        return Verify(decompile, GetSettings());
    }
#endif

    VerifySettings GetSettings()
    {
        var settings = new VerifySettings();
        settings.AutoVerify();
        settings.UniqueForRuntimeAndVersion();
        return settings;
    }
}