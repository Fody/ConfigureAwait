#if NET
public partial class ModuleWeaverTests
{
    // Issue #624: the async iterator body must be woven so its awaits do not resume on
    // the captured context.
    [Fact]
    public async Task AsyncEnumerable_Producer()
    {
        var context = testResult.GetInstance("FlagSynchronizationContext");
        var test = testResult.GetInstance("AsyncEnumerable");

        Assert.False(context.Flag);

        var enumerable = (IAsyncEnumerable<int>)test.Producer(context);
        // ReSharper disable once UnusedVariable
        await foreach (var item in enumerable)
        {
        }

        Assert.False(context.Flag);
    }

    // Issue #597: `await foreach` over an IAsyncEnumerable<T>.
    [Fact]
    public async Task AsyncEnumerable_AwaitForeach()
    {
        var context = testResult.GetInstance("FlagSynchronizationContext");
        var test = testResult.GetInstance("AsyncEnumerable");

        Assert.False(context.Flag);

        await test.AwaitForeach(context);

        Assert.False(context.Flag);
    }

    // Issue #597: `await using` over an IAsyncDisposable.
    [Fact]
    public async Task AsyncEnumerable_AwaitUsing()
    {
        var context = testResult.GetInstance("FlagSynchronizationContext");
        var test = testResult.GetInstance("AsyncEnumerable");

        Assert.False(context.Flag);

        await test.AwaitUsing(context);

        Assert.False(context.Flag);
    }
}
#endif
