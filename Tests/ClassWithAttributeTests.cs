public partial class ModuleWeaverTests
{
    [Fact]
    public async Task ClassWithAttribute_AsyncMethod()
    {
        var context = testResult.GetInstance("FlagSynchronizationContext");
        var test = testResult.GetInstance("ClassWithAttribute");

        Assert.False(context.Flag);

        await test.AsyncMethod(context);

        Assert.False(context.Flag);
    }

    [Fact]
    public async Task ClassWithAttribute_AsyncMethodWithReturn()
    {
        var context = testResult.GetInstance("FlagSynchronizationContext");
        var test = testResult.GetInstance("ClassWithAttribute");

        Assert.False(context.Flag);

        var result = await test.AsyncMethodWithReturn(context);

        Assert.False(context.Flag);
        Assert.Equal(10, result);
    }

    [Fact]
    public async Task ClassWithAttribute_AsyncGenericMethod()
    {
        var context = testResult.GetInstance("FlagSynchronizationContext");
        var test = testResult.GetInstance("ClassWithAttribute");

        Assert.False(context.Flag);

        await test.AsyncGenericMethod(context);

        Assert.False(context.Flag);
    }

    [Fact]
    public async Task ClassWithAttribute_AsyncGenericMethodWithReturn()
    {
        var context = testResult.GetInstance("FlagSynchronizationContext");
        var test = testResult.GetInstance("ClassWithAttribute");

        Assert.False(context.Flag);

        var result = await test.AsyncGenericMethodWithReturn(context);

        Assert.False(context.Flag);
        Assert.Equal(10, result);
    }

#if NET
    [Fact]
    public async Task ClassWithAttribute_AsyncMethod_WithValueTask()
    {
        var context = testResult.GetInstance("FlagSynchronizationContext");
        var test = testResult.GetInstance("ClassWithAttribute");

        Assert.False(context.Flag);

        await test.AsyncMethod_WithValueTask(context);

        Assert.False(context.Flag);
    }

    [Fact]
    public async Task ClassWithAttribute_AsyncMethodWithReturn_WithValueTask()
    {
        var context = testResult.GetInstance("FlagSynchronizationContext");
        var test = testResult.GetInstance("ClassWithAttribute");

        Assert.False(context.Flag);

        var result = await test.AsyncMethodWithReturn_WithValueTask(context);

        Assert.False(context.Flag);
        Assert.Equal(10, result);
    }

    [Fact]
    public async Task ClassWithAttribute_AsyncGenericMethod_WithValueTask()
    {
        var context = testResult.GetInstance("FlagSynchronizationContext");
        var test = testResult.GetInstance("ClassWithAttribute");

        Assert.False(context.Flag);

        await test.AsyncGenericMethod_WithValueTask(context);

        Assert.False(context.Flag);
    }

    [Fact]
    public async Task ClassWithAttribute_AsyncGenericMethodWithReturn_WithValueTask()
    {
        var context = testResult.GetInstance("FlagSynchronizationContext");
        var test = testResult.GetInstance("ClassWithAttribute");

        Assert.False(context.Flag);

        var result = await test.AsyncGenericMethodWithReturn_WithValueTask(context);

        Assert.False(context.Flag);
        Assert.Equal(10, result);
    }
#endif
}