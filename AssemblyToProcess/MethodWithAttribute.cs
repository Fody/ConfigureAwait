using Fody;

public class MethodWithAttribute
{
    [ConfigureAwait(false)]
    public async Task AsyncMethod(SynchronizationContext context)
    {
        SynchronizationContext.SetSynchronizationContext(context);
        await Task.Delay(0);
    }

#if NETCOREAPP2_0
    [ConfigureAwait(false)]
    public async Task AsyncMethod_WithValueTask(SynchronizationContext context)
    {
        SynchronizationContext.SetSynchronizationContext(context);
        await new ValueTask(Task.Delay(0));
    }
#endif
}