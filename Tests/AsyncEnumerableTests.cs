#if NET
public partial class ModuleWeaverTests
{
    // Issue #624: the async iterator body must be woven so its awaits do not resume on
    // the captured context.
    [Test]
    public async Task AsyncEnumerable_Producer()
    {
        var context = testResult.GetInstance("FlagSynchronizationContext");
        var test = testResult.GetInstance("AsyncEnumerable");

        await Assert.That((bool)context.Flag).IsFalse();

        var enumerable = (IAsyncEnumerable<int>)test.Producer(context);
        // ReSharper disable once UnusedVariable
        await foreach (var item in enumerable)
        {
        }

        await Assert.That((bool)context.Flag).IsFalse();
    }

    // Issue #597: `await foreach` over an IAsyncEnumerable<T>.
    [Test]
    public async Task AsyncEnumerable_AwaitForeach()
    {
        var context = testResult.GetInstance("FlagSynchronizationContext");
        var test = testResult.GetInstance("AsyncEnumerable");

        await Assert.That((bool)context.Flag).IsFalse();

        await test.AwaitForeach(context);

        await Assert.That((bool)context.Flag).IsFalse();
    }

    // Issue #597: `await using` over an IAsyncDisposable.
    [Test]
    public async Task AsyncEnumerable_AwaitUsing()
    {
        var context = testResult.GetInstance("FlagSynchronizationContext");
        var test = testResult.GetInstance("AsyncEnumerable");

        await Assert.That((bool)context.Flag).IsFalse();

        await test.AwaitUsing(context);

        await Assert.That((bool)context.Flag).IsFalse();
    }
}
#endif
