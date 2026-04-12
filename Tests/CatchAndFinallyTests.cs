public partial class ModuleWeaverTests
{
    [Fact]
    public async Task CatchAndFinally_Catch1()
    {
        var test = testResult.GetInstance("CatchAndFinally");

        await test.Catch1();
    }

    [Fact]
    public async Task CatchAndFinally_Catch2()
    {
        var context = testResult.GetInstance("FlagSynchronizationContext");
        var test = testResult.GetInstance("CatchAndFinally");

        SynchronizationContext.SetSynchronizationContext((SynchronizationContext)context);
        Task task;
        try
        {
            task = test.Catch2();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(null);
        }

        await task;

        Assert.False(context.Flag);
    }

    [Fact]
    public async Task CatchAndFinally_Catch3()
    {
        var test = testResult.GetInstance("CatchAndFinally");

        await test.Catch3();
    }

    [Fact]
    public async Task CatchAndFinally_Finally1()
    {
        var test = testResult.GetInstance("CatchAndFinally");

        await Assert.ThrowsAsync<NotImplementedException>(() => (Task)test.Finally1());
    }

    [Fact]
    public async Task CatchAndFinally_Finally2()
    {
        var context = testResult.GetInstance("FlagSynchronizationContext");
        var test = testResult.GetInstance("CatchAndFinally");

        SynchronizationContext.SetSynchronizationContext((SynchronizationContext)context);
        Task task;
        try
        {
            task = test.Finally2();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(null);
        }

        await Assert.ThrowsAsync<NotImplementedException>(() => task);

        Assert.False(context.Flag);
    }

    [Fact]
    public async Task CatchAndFinally_Finally3()
    {
        var test = testResult.GetInstance("CatchAndFinally");

        await Assert.ThrowsAsync<NotImplementedException>(() => (Task)test.Finally3());
    }

#if NET
    [Fact]
    public async Task CatchAndFinally_Catch1_WithValueTask()
    {
        var test = testResult.GetInstance("CatchAndFinally");

        await test.Catch1_WithValueTask();
    }

    [Fact]
    public async Task CatchAndFinally_Catch2_WithValueTask()
    {
        var context = testResult.GetInstance("FlagSynchronizationContext");
        var test = testResult.GetInstance("CatchAndFinally");

        SynchronizationContext.SetSynchronizationContext((SynchronizationContext)context);
        Task task;
        try
        {
            task = test.Catch2_WithValueTask();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(null);
        }

        await task;

        Assert.False(context.Flag);
    }

    [Fact]
    public async Task CatchAndFinally_Catch3_WithValueTask()
    {
        var test = testResult.GetInstance("CatchAndFinally");

        await test.Catch3_WithValueTask();
    }

    [Fact]
    public async Task CatchAndFinally_Finally1_WithValueTask()
    {
        var test = testResult.GetInstance("CatchAndFinally");

        await Assert.ThrowsAsync<NotImplementedException>(() => (Task)test.Finally1_WithValueTask());
    }

    [Fact]
    public async Task CatchAndFinally_Finally2_WithValueTask()
    {
        var context = testResult.GetInstance("FlagSynchronizationContext");
        var test = testResult.GetInstance("CatchAndFinally");

        SynchronizationContext.SetSynchronizationContext((SynchronizationContext)context);
        Task task;
        try
        {
            task = test.Finally2_WithValueTask();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(null);
        }

        await Assert.ThrowsAsync<NotImplementedException>(() => task);

        Assert.False(context.Flag);
    }

    [Fact]
    public async Task CatchAndFinally_Finally3_WithValueTask()
    {
        var test = testResult.GetInstance("CatchAndFinally");

        await Assert.ThrowsAsync<NotImplementedException>(() => (Task)test.Finally3_WithValueTask());
    }
#endif
}
