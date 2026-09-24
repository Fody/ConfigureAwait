public partial class ModuleWeaverTests
{
    [Test]
    public async Task CatchAndFinally_Catch1()
    {
        var test = testResult.GetInstance("CatchAndFinally");

        await test.Catch1();
    }

    [Test]
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

        await Assert.That((bool)context.Flag).IsFalse();
    }

    [Test]
    public async Task CatchAndFinally_Catch3()
    {
        var test = testResult.GetInstance("CatchAndFinally");

        await test.Catch3();
    }

    [Test]
    public async Task CatchAndFinally_Finally1()
    {
        var test = testResult.GetInstance("CatchAndFinally");

        await Assert.That(async () => await (Task)test.Finally1()).Throws<NotImplementedException>();
    }

    [Test]
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

        await Assert.That(async () => await task).Throws<NotImplementedException>();

        await Assert.That((bool)context.Flag).IsFalse();
    }

    [Test]
    public async Task CatchAndFinally_Finally3()
    {
        var test = testResult.GetInstance("CatchAndFinally");

        await Assert.That(async () => await (Task)test.Finally3()).Throws<NotImplementedException>();
    }

#if NET
    [Test]
    public async Task CatchAndFinally_Catch1_WithValueTask()
    {
        var test = testResult.GetInstance("CatchAndFinally");

        await test.Catch1_WithValueTask();
    }

    [Test]
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

        await Assert.That((bool)context.Flag).IsFalse();
    }

    [Test]
    public async Task CatchAndFinally_Catch3_WithValueTask()
    {
        var test = testResult.GetInstance("CatchAndFinally");

        await test.Catch3_WithValueTask();
    }

    [Test]
    public async Task CatchAndFinally_Finally1_WithValueTask()
    {
        var test = testResult.GetInstance("CatchAndFinally");

        await Assert.That(async () => await (Task)test.Finally1_WithValueTask()).Throws<NotImplementedException>();
    }

    [Test]
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

        await Assert.That(async () => await task).Throws<NotImplementedException>();

        await Assert.That((bool)context.Flag).IsFalse();
    }

    [Test]
    public async Task CatchAndFinally_Finally3_WithValueTask()
    {
        var test = testResult.GetInstance("CatchAndFinally");

        await Assert.That(async () => await (Task)test.Finally3_WithValueTask()).Throws<NotImplementedException>();
    }
#endif
}
