using System.Reflection;
using Fody;
using VerifyTests;
using VerifyXunit;
using Xunit.Abstractions;

[UsesVerify]
public partial class ModuleWeaverTests
{
    static TestResult testResult;

    static ModuleWeaverTests()
    {
        Assembly.Load("xunit.assert");
        var weavingTask = new ModuleWeaver();
        testResult = weavingTask.ExecuteTestRun("AssemblyToProcess.dll");
    }

    public ModuleWeaverTests(ITestOutputHelper output)
    {
        VerifierSettings.UniqueForRuntime();
    }
}