public partial class ModuleWeaverTests
{
    [Fact]
    public async Task DoNotWeave_AsyncMethod()
    {
        var context = testResult.GetInstance("FlagSynchronizationContext");
        var test = testResult.GetInstance("DoNotWeave");

        Assert.False(context.Flag);

        await test.AsyncMethod(context);

        Assert.True(context.Flag);
    }

    [Fact]
    public async Task DoNotWeave_AsyncMethodWithReturn()
    {
        var context = testResult.GetInstance("FlagSynchronizationContext");
        var test = testResult.GetInstance("DoNotWeave");

        Assert.False(context.Flag);

        var result = await test.AsyncMethodWithReturn(context);

        Assert.True(context.Flag);
        Assert.Equal(10, result);
    }

    [Fact]
    public async Task DoNotWeave_AsyncGenericMethod()
    {
        var context = testResult.GetInstance("FlagSynchronizationContext");
        var test = testResult.GetInstance("DoNotWeave");

        Assert.False(context.Flag);

        await test.AsyncGenericMethod(context);

        Assert.True(context.Flag);
    }

    [Fact]
    public async Task DoNotWeave_AsyncGenericMethodWithReturn()
    {
        var context = testResult.GetInstance("FlagSynchronizationContext");
        var test = testResult.GetInstance("DoNotWeave");

        Assert.False(context.Flag);

        var result = await test.AsyncGenericMethodWithReturn(context);

        Assert.True(context.Flag);
        Assert.Equal(10, result);
    }
}