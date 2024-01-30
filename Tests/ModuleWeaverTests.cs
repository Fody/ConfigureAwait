using System.Reflection;
using Fody;

public partial class ModuleWeaverTests
{
    static TestResult testResult;

    static ModuleWeaverTests()
    {
        Assembly.Load("xunit.assert");
        var weaver = new ModuleWeaver();
        testResult = weaver.ExecuteTestRun("AssemblyToProcess.dll");
        VerifierSettings.UniqueForRuntime();
        VerifierSettings.UniqueForAssemblyConfiguration();
    }
}