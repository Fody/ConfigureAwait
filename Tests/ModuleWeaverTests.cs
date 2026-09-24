using Fody;
using TestResult = Fody.TestResult;

// the tests share the static weaver result and set the synchronization context
[NotInParallel]
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