public partial class ModuleWeaverTests
{
    [Test]
    public async Task MethodWithAttribute_AsyncMethod()
    {
        var context = testResult.GetInstance("FlagSynchronizationContext");
        var test = testResult.GetInstance("MethodWithAttribute");

        await Assert.That((bool)context.Flag).IsFalse();

        await test.AsyncMethod(context);

        await Assert.That((bool)context.Flag).IsFalse();
    }

#if NET
    [Test]
    public async Task MethodWithAttribute_AsyncMethod_WithValueTask()
    {
        var context = testResult.GetInstance("FlagSynchronizationContext");
        var test = testResult.GetInstance("MethodWithAttribute");

        await Assert.That((bool)context.Flag).IsFalse();

        await test.AsyncMethod_WithValueTask(context);

        await Assert.That((bool)context.Flag).IsFalse();
    }
#endif
}