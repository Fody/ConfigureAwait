using VerifyTUnit;
using Fody;
#pragma warning disable CS0618

public partial class ModuleWeaverTests
{
    [Test]
    public async Task DecompileExample()
    {
        var decompile = Ildasm.Decompile(testResult.AssemblyPath, "Example");
        await Verifier.Verify(decompile, GetSettings());
    }

    [Test]
    public async Task DecompileIssue1()
    {
        var decompile = Ildasm.Decompile(testResult.AssemblyPath, "Issue1");
        await Verifier.Verify(decompile, GetSettings());
    }

    [Test]
    public async Task DecompileGenericClass()
    {
        var decompile = Ildasm.Decompile(testResult.AssemblyPath, "GenericClass`1");
        await Verifier.Verify(decompile, GetSettings());
    }

    [Test]
    public async Task DecompileGenericMethod()
    {
        var decompile = Ildasm.Decompile(testResult.AssemblyPath, "GenericMethod");
        await Verifier.Verify(decompile, GetSettings());
    }

    [Test]
    public async Task DecompileCatchAndFinally()
    {
        var decompile = Ildasm.Decompile(testResult.AssemblyPath, "CatchAndFinally");
        await Verifier.Verify(decompile, GetSettings());
    }

    [Test]
    public async Task DecompileAwaitTernary()
    {
        var decompile = Ildasm.Decompile(testResult.AssemblyPath, "AwaitTernary");
        await Verifier.Verify(decompile, GetSettings());
    }

#if NET
    [Test]
    public async Task DecompileAsyncEnumerable()
    {
        var decompile = Ildasm.Decompile(testResult.AssemblyPath, "AsyncEnumerable");
        await Verifier.Verify(decompile, GetSettings());
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