using System.Reflection;
using Fody;
using VerifyTests;
using VerifyXunit;

[UsesVerify]
public partial class ModuleWeaverTests
{
    static TestResult testResult;

    static ModuleWeaverTests()
    {
        Assembly.Load("xunit.assert");
        var weavingTask = new ModuleWeaver();
        testResult = weavingTask.ExecuteTestRun("AssemblyToProcess.dll");
        VerifierSettings.UniqueForRuntime();
        VerifierSettings.UniqueForAssemblyConfiguration();
    }
}