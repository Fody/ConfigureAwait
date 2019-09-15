using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
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
        var targetFrameworkAttribute = (TargetFrameworkAttribute)Assembly.GetExecutingAssembly()
            .GetCustomAttributes(typeof(TargetFrameworkAttribute), false)
            .SingleOrDefault();
        var framework = targetFrameworkAttribute.FrameworkDisplayName.Split(' ').Last();
#if DEBUG
        var suffix = "Debug" + framework;
#else
        var suffix = "Release" + framework;
#endif 
        disposable = NamerFactory.AsEnvironmentSpecificTest(() => suffix);
    }

    public override void Dispose()
    {
        base.Dispose();
        disposable.Dispose();
    }
}