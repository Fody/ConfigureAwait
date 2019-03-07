using System;
using System.Reflection;
using ApprovalTests.Namers;
using Fody;
#pragma warning disable 618

public partial class ModuleWeaverTests:IDisposable
{
    static TestResult testResult;
    IDisposable disposable;

    static ModuleWeaverTests()
    {
        Assembly.Load("xunit.assert");
        var weavingTask = new ModuleWeaver();
        testResult = weavingTask.ExecuteTestRun("AssemblyToProcess.dll");
    }

    public ModuleWeaverTests()
    {
#if DEBUG
        disposable = NamerFactory.AsEnvironmentSpecificTest(() => "Debug"+ApprovalResults.GetDotNetRuntime(true));
#else
        disposable = NamerFactory.AsEnvironmentSpecificTest(() => "Release"+ApprovalResults.GetDotNetRuntime(true));
#endif
    }

    public void Dispose()
    {
        disposable.Dispose();
    }
}