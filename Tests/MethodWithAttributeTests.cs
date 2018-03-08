using System.Threading.Tasks;
using Xunit;

public partial class ModuleWeaverTests
{
    [Fact]
    public async Task MethodWithAttribute_AsyncMethod()
    {
        var context = testResult.GetInstance("AssemblyToProcess.FlagSyncronizationContext");
        var test = testResult.GetInstance("AssemblyToProcess.MethodWithAttribute");

        Assert.False(context.Flag);

        await test.AsyncMethod(context);

        Assert.False(context.Flag);
    }
}