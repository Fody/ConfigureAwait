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

#if NET
    [Fact]
    public async Task DoNotWeave_AsyncMethod_WithValueTask()
    {
        var context = testResult.GetInstance("FlagSynchronizationContext");
        var test = testResult.GetInstance("DoNotWeave");

        Assert.False(context.Flag);

        await test.AsyncMethod_WithValueTask(context);

        Assert.True(context.Flag);
    }

    [Fact]
    public async Task DoNotWeave_AsyncMethodWithReturn_WithValueTask()
    {
        var context = testResult.GetInstance("FlagSynchronizationContext");
        var test = testResult.GetInstance("DoNotWeave");

        Assert.False(context.Flag);

        var result = await test.AsyncMethodWithReturn_WithValueTask(context);

        Assert.True(context.Flag);
        Assert.Equal(10, result);
    }

    [Fact]
    public async Task DoNotWeave_AsyncGenericMethod_WithValueTask()
    {
        var context = testResult.GetInstance("FlagSynchronizationContext");
        var test = testResult.GetInstance("DoNotWeave");

        Assert.False(context.Flag);

        await test.AsyncGenericMethod_WithValueTask(context);

        Assert.True(context.Flag);
    }

#if !NET11_0
    
    // Net11:
    // Even though the method is really excluded from weaving, this test fails, but only if running all tests at once. Running only this test succeeds.
    // Exclude this false positive for now.
    
    [Fact]
    public async Task DoNotWeave_AsyncGenericMethodWithReturn_WithValueTask()
    {
        var context = testResult.GetInstance("FlagSynchronizationContext");
        var test = testResult.GetInstance("DoNotWeave");

        Assert.False(context.Flag);

        var result = await test.AsyncGenericMethodWithReturn_WithValueTask(context);

        Assert.True(context.Flag);
        Assert.Equal(10, result);
    }
#endif

#endif
}