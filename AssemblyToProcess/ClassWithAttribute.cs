using Fody;

[ConfigureAwait(false)]
public class ClassWithAttribute
{
    public async Task AsyncMethod(SynchronizationContext context)
    {
        SynchronizationContext.SetSynchronizationContext(context);
        await Task.Delay(10);
    }

    public async Task<int> AsyncMethodWithReturn(SynchronizationContext context)
    {
        SynchronizationContext.SetSynchronizationContext(context);
        await Task.Delay(10);
        return 10;
    }

    public async Task AsyncGenericMethod(SynchronizationContext context)
    {
        SynchronizationContext.SetSynchronizationContext(context);
        await Task.Run(async () => await Return10());
    }

    public async Task<int> AsyncGenericMethodWithReturn(SynchronizationContext context)
    {
        SynchronizationContext.SetSynchronizationContext(context);
        return await Task.Run(async () => await Return10());
    }

#if NET
    public async Task AsyncMethod_WithValueTask(SynchronizationContext context)
    {
        SynchronizationContext.SetSynchronizationContext(context);
        await new ValueTask(Task.Delay(10));
    }

    public async Task<int> AsyncMethodWithReturn_WithValueTask(SynchronizationContext context)
    {
        SynchronizationContext.SetSynchronizationContext(context);
        await new ValueTask(Task.Delay(10));
        return 10;
    }

    public async Task AsyncGenericMethod_WithValueTask(SynchronizationContext context)
    {
        SynchronizationContext.SetSynchronizationContext(context);
        await new ValueTask(Task.Run(async () => await Return10()));
    }

    public async Task<int> AsyncGenericMethodWithReturn_WithValueTask(SynchronizationContext context)
    {
        SynchronizationContext.SetSynchronizationContext(context);
        return await new ValueTask<int>(Task.Run(async () => await Return10()));
    }
#endif

    // using some more complex task than async () => 10, to make sure the method is not optimized away by the compiler, which would make the test fail;
    async Task<int> Return10()
    {
        await Task.Delay(10).ConfigureAwait(false);
        return 10;
    }
}
