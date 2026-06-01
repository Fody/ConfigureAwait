#if NET
using Fody;

[ConfigureAwait(false)]
public class AsyncEnumerable
{
    // Issue #624: an async iterator method (lowered with AsyncIteratorStateMachineAttribute)
    // must have its internal awaits configured. The only await here is the unconfigured
    // Task.Delay, so the flag reflects whether the iterator body was woven.
    public async IAsyncEnumerable<int> Producer(SynchronizationContext context)
    {
        SynchronizationContext.SetSynchronizationContext(context);
        await Task.Delay(10);
        yield return 1;
        await Task.Delay(10);
        yield return 2;
    }

    // Issue #597: consuming an IAsyncEnumerable<T> with `await foreach` awaits the
    // ValueTask<bool>/ValueTask returned by MoveNextAsync/DisposeAsync. The source's own
    // await is configured explicitly so only this consumer's awaits are under test.
    public async Task AwaitForeach(SynchronizationContext context)
    {
        SynchronizationContext.SetSynchronizationContext(context);
        // ReSharper disable once UnusedVariable
        await foreach (var item in Source())
        {
        }
    }

    static async IAsyncEnumerable<int> Source()
    {
        await Task.Delay(10).ConfigureAwait(false);
        yield return 1;
    }

    // Issue #597: consuming an IAsyncDisposable with `await using` awaits the ValueTask
    // returned by DisposeAsync. The disposable's own await is configured explicitly so
    // only this consumer's await is under test.
    public async Task AwaitUsing(SynchronizationContext context)
    {
        SynchronizationContext.SetSynchronizationContext(context);
        await using (new AsyncDisposable())
        {
        }
    }
}

public class AsyncDisposable : IAsyncDisposable
{
    public async ValueTask DisposeAsync()
    {
        await Task.Delay(10).ConfigureAwait(false);
        Disposed = true;
    }

    public static bool Disposed;
}
#endif
