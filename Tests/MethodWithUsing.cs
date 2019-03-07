using System.Threading.Tasks;
using Xunit;

public partial class ModuleWeaverTests
{
    [Fact]
    public async Task MethodWithUsing()
    {
        var test = testResult.GetInstance("AssemblyToProcess.MethodWithUsing");
        await test.AsyncMethod();
        var disposableType = testResult.Assembly.GetType("AssemblyToProcess.MyDisposable");
        var disposedField = disposableType.GetField("Disposed");
        Assert.True((bool)disposedField.GetValue(null));
    }
}