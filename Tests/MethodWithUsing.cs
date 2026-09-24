public partial class ModuleWeaverTests
{
    [Test]
    public async Task MethodWithUsing()
    {
        var test = testResult.GetInstance("MethodWithUsing");
        await test.AsyncMethod();
        var disposableType = testResult.Assembly.GetType("MyDisposable");
        var disposedField = disposableType.GetField("Disposed");
        await Assert.That((bool)disposedField.GetValue(null)).IsTrue();
    }

#if NET
    [Test]
    public async Task MethodWithUsing_WithValueTask()
    {
        var test = testResult.GetInstance("MethodWithUsing");
        await test.AsyncMethod_WithValueTask();
        var disposableType = testResult.Assembly.GetType("MyDisposable");
        var disposedField = disposableType.GetField("Disposed");
        await Assert.That((bool)disposedField.GetValue(null)).IsTrue();
    }
#endif
}