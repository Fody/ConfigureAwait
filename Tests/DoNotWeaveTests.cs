public partial class ModuleWeaverTests
{
    [Test]
    public async Task DoNotWeave_AsyncMethod()
    {
        var context = testResult.GetInstance("FlagSynchronizationContext");
        var test = testResult.GetInstance("DoNotWeave");

        await Assert.That((bool)context.Flag).IsFalse();

        await test.AsyncMethod(context);

        await Assert.That((bool)context.Flag).IsTrue();
    }

    [Test]
    public async Task DoNotWeave_AsyncMethodWithReturn()
    {
        var context = testResult.GetInstance("FlagSynchronizationContext");
        var test = testResult.GetInstance("DoNotWeave");

        await Assert.That((bool)context.Flag).IsFalse();

        var result = await test.AsyncMethodWithReturn(context);

        await Assert.That((bool)context.Flag).IsTrue();
        await Assert.That((int)result).IsEqualTo(10);
    }

    [Test]
    public async Task DoNotWeave_AsyncGenericMethod()
    {
        var context = testResult.GetInstance("FlagSynchronizationContext");
        var test = testResult.GetInstance("DoNotWeave");

        await Assert.That((bool)context.Flag).IsFalse();

        await test.AsyncGenericMethod(context);

        await Assert.That((bool)context.Flag).IsTrue();
    }

    [Test]
    public async Task DoNotWeave_AsyncGenericMethodWithReturn()
    {
        var context = testResult.GetInstance("FlagSynchronizationContext");
        var test = testResult.GetInstance("DoNotWeave");

        await Assert.That((bool)context.Flag).IsFalse();

        var result = await test.AsyncGenericMethodWithReturn(context);

        await Assert.That((bool)context.Flag).IsTrue();
        await Assert.That((int)result).IsEqualTo(10);
    }

#if NET
    [Test]
    public async Task DoNotWeave_AsyncMethod_WithValueTask()
    {
        var context = testResult.GetInstance("FlagSynchronizationContext");
        var test = testResult.GetInstance("DoNotWeave");

        await Assert.That((bool)context.Flag).IsFalse();

        await test.AsyncMethod_WithValueTask(context);

        await Assert.That((bool)context.Flag).IsTrue();
    }

    [Test]
    public async Task DoNotWeave_AsyncMethodWithReturn_WithValueTask()
    {
        var context = testResult.GetInstance("FlagSynchronizationContext");
        var test = testResult.GetInstance("DoNotWeave");

        await Assert.That((bool)context.Flag).IsFalse();

        var result = await test.AsyncMethodWithReturn_WithValueTask(context);

        await Assert.That((bool)context.Flag).IsTrue();
        await Assert.That((int)result).IsEqualTo(10);
    }

    [Test]
    public async Task DoNotWeave_AsyncGenericMethod_WithValueTask()
    {
        var context = testResult.GetInstance("FlagSynchronizationContext");
        var test = testResult.GetInstance("DoNotWeave");

        await Assert.That((bool)context.Flag).IsFalse();

        await test.AsyncGenericMethod_WithValueTask(context);

        await Assert.That((bool)context.Flag).IsTrue();
    }

    [Test]
    public async Task DoNotWeave_AsyncGenericMethodWithReturn_WithValueTask()
    {
        var context = testResult.GetInstance("FlagSynchronizationContext");
        var test = testResult.GetInstance("DoNotWeave");

        await Assert.That((bool)context.Flag).IsFalse();

        var result = await test.AsyncGenericMethodWithReturn_WithValueTask(context);

        await Assert.That((bool)context.Flag).IsTrue();
        await Assert.That((int)result).IsEqualTo(10);
    }

#endif

}