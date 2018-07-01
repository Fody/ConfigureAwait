using System.Reflection;
using ConfigureAwait.Fody;
using Fody;
#pragma warning disable 618

#if NET46
[ApprovalTests.Namers.UseApprovalSubdirectory("ApprovalFiles")]
#endif
public partial class ModuleWeaverTests
{
    static TestResult testResult;

    static ModuleWeaverTests()
    {
        Assembly.Load("xunit.assert");
        var weavingTask = new ModuleWeaver();
        testResult = weavingTask.ExecuteTestRun("AssemblyToProcess.dll");
#if NET46
#if DEBUG
        ApprovalTests.Namers.NamerFactory.AsEnvironmentSpecificTest(() => "Debug");
#else
        ApprovalTests.Namers.NamerFactory.AsEnvironmentSpecificTest(() => "Release");
#endif
#endif
    }
}