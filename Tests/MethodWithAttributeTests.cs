public partial class ModuleWeaverTests
{
    [Fact]
    public async Task MethodWithAttribute_AsyncMethod()
    {
        var context = testResult.GetInstance("FlagSynchronizationContext");
        var test = testResult.GetInstance("MethodWithAttribute");

        Assert.False(context.Flag);

        await test.AsyncMethod(context);

        Assert.False(context.Flag);
    }

#if NET
    [Fact]
    public async Task MethodWithAttribute_AsyncMethod_WithValueTask()
    {
        var context = testResult.GetInstance("FlagSynchronizationContext");
        var test = testResult.GetInstance("MethodWithAttribute");

        Assert.False(context.Flag);

        await test.AsyncMethod_WithValueTask(context);

        Assert.False(context.Flag);
    }
#endif
}