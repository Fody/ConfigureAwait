using System.Reflection;
using Fody;
using Verify;
using VerifyXunit;
using Xunit.Abstractions;

public partial class ModuleWeaverTests:
    VerifyBase
{
    static TestResult testResult;

    static ModuleWeaverTests()
    {
        Assembly.Load("xunit.assert");
        var weavingTask = new ModuleWeaver();
        testResult = weavingTask.ExecuteTestRun("AssemblyToProcess.dll");
    }

    public ModuleWeaverTests(ITestOutputHelper output) :
        base(output)
    {
        SharedVerifySettings.UniqueForRuntime();
    }
}