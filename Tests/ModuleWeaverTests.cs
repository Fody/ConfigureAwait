using Fody;
using TestResult = Fody.TestResult;

public partial class ModuleWeaverTests
{
    static TestResult testResult;

    static ModuleWeaverTests()
    {
        var weaver = new ModuleWeaver();
        testResult = weaver.ExecuteTestRun("AssemblyToProcess.dll");
        VerifierSettings.UniqueForRuntimeAndVersion();
        VerifierSettings.UniqueForAssemblyConfiguration();
    }
}