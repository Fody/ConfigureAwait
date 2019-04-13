using System;
using System.Reflection;
using ApprovalTests.Namers;
using Fody;
using Xunit.Abstractions;

public partial class ModuleWeaverTests
{
    static TestResult testResult;
    IDisposable disposable;

    static ModuleWeaverTests()
    {
        Assembly.Load("xunit.assert");
        var weavingTask = new ModuleWeaver();
        testResult = weavingTask.ExecuteTestRun("AssemblyToProcess.dll");
    }

    public ModuleWeaverTests(ITestOutputHelper output) : 
        base(output)
    {
#if DEBUG
        disposable = NamerFactory.AsEnvironmentSpecificTest(() => "Debug"+ApprovalResults.GetDotNetRuntime(true));
#else
        disposable = NamerFactory.AsEnvironmentSpecificTest(() => "Release"+ApprovalResults.GetDotNetRuntime(true));
#endif
    }

    public override void Dispose()
    {
        base.Dispose();
        disposable.Dispose();
    }
}