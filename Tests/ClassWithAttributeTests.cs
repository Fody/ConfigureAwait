public partial class ModuleWeaverTests
{
    [Test]
    public async Task ClassWithAttribute_AsyncMethod()
    {
        var context = testResult.GetInstance("FlagSynchronizationContext");
        var test = testResult.GetInstance("ClassWithAttribute");

        await Assert.That((bool)context.Flag).IsFalse();

        await test.AsyncMethod(context);

        await Assert.That((bool)context.Flag).IsFalse();
    }

    [Test]
    public async Task ClassWithAttribute_AsyncMethodWithReturn()
    {
        var context = testResult.GetInstance("FlagSynchronizationContext");
        var test = testResult.GetInstance("ClassWithAttribute");

        await Assert.That((bool)context.Flag).IsFalse();

        var result = await test.AsyncMethodWithReturn(context);

        await Assert.That((bool)context.Flag).IsFalse();
        await Assert.That((int)result).IsEqualTo(10);
    }

    [Test]
    public async Task ClassWithAttribute_AsyncGenericMethod()
    {
        var context = testResult.GetInstance("FlagSynchronizationContext");
        var test = testResult.GetInstance("ClassWithAttribute");

        await Assert.That((bool)context.Flag).IsFalse();

        await test.AsyncGenericMethod(context);

        await Assert.That((bool)context.Flag).IsFalse();
    }

    [Test]
    public async Task ClassWithAttribute_AsyncGenericMethodWithReturn()
    {
        var context = testResult.GetInstance("FlagSynchronizationContext");
        var test = testResult.GetInstance("ClassWithAttribute");

        await Assert.That((bool)context.Flag).IsFalse();

        var result = await test.AsyncGenericMethodWithReturn(context);

        await Assert.That((bool)context.Flag).IsFalse();
        await Assert.That((int)result).IsEqualTo(10);
    }

#if NET
    [Test]
    public async Task ClassWithAttribute_AsyncMethod_WithValueTask()
    {
        var context = testResult.GetInstance("FlagSynchronizationContext");
        var test = testResult.GetInstance("ClassWithAttribute");

        await Assert.That((bool)context.Flag).IsFalse();

        await test.AsyncMethod_WithValueTask(context);

        await Assert.That((bool)context.Flag).IsFalse();
    }

    [Test]
    public async Task ClassWithAttribute_AsyncMethodWithReturn_WithValueTask()
    {
        var context = testResult.GetInstance("FlagSynchronizationContext");
        var test = testResult.GetInstance("ClassWithAttribute");

        await Assert.That((bool)context.Flag).IsFalse();

        var result = await test.AsyncMethodWithReturn_WithValueTask(context);

        await Assert.That((bool)context.Flag).IsFalse();
        await Assert.That((int)result).IsEqualTo(10);
    }

    [Test]
    public async Task ClassWithAttribute_AsyncGenericMethod_WithValueTask()
    {
        var context = testResult.GetInstance("FlagSynchronizationContext");
        var test = testResult.GetInstance("ClassWithAttribute");

        await Assert.That((bool)context.Flag).IsFalse();

        await test.AsyncGenericMethod_WithValueTask(context);

        await Assert.That((bool)context.Flag).IsFalse();
    }

    [Test]
    public async Task ClassWithAttribute_AsyncGenericMethodWithReturn_WithValueTask()
    {
        var context = testResult.GetInstance("FlagSynchronizationContext");
        var test = testResult.GetInstance("ClassWithAttribute");

        await Assert.That((bool)context.Flag).IsFalse();

        var result = await test.AsyncGenericMethodWithReturn_WithValueTask(context);

        await Assert.That((bool)context.Flag).IsFalse();
        await Assert.That((int)result).IsEqualTo(10);
    }
#endif
}