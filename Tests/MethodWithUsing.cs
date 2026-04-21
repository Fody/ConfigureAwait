public partial class ModuleWeaverTests
{
    [Fact]
    public async Task MethodWithUsing()
    {
        var test = testResult.GetInstance("MethodWithUsing");
        await test.AsyncMethod();
        var disposableType = testResult.Assembly.GetType("MyDisposable");
        var disposedField = disposableType.GetField("Disposed");
        Assert.True((bool)disposedField.GetValue(null));
    }

#if NET
    [Fact]
    public async Task MethodWithUsing_WithValueTask()
    {
        var test = testResult.GetInstance("MethodWithUsing");
        await test.AsyncMethod_WithValueTask();
        var disposableType = testResult.Assembly.GetType("MyDisposable");
        var disposedField = disposableType.GetField("Disposed");
        Assert.True((bool)disposedField.GetValue(null));
    }
#endif
}