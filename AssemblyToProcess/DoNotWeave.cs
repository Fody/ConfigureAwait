using System.Threading;
using System.Threading.Tasks;
using Fody;

namespace AssemblyToProcess
{
    [ConfigureAwait(false)]
    public class DoNotWeave
    {
        public async Task AsyncMethod(SynchronizationContext context)
        {
            SynchronizationContext.SetSynchronizationContext(context);
            await Task.Delay(10).ConfigureAwait(true);
        }

        public async Task<int> AsyncMethodWithReturn(SynchronizationContext context)
        {
            SynchronizationContext.SetSynchronizationContext(context);
            await Task.Delay(10).ConfigureAwait(true);
            return 10;
        }

        public async Task AsyncGenericMethod(SynchronizationContext context)
        {
            SynchronizationContext.SetSynchronizationContext(context);
            await Task.Run(() => 10).ConfigureAwait(true);
        }

        public async Task<int> AsyncGenericMethodWithReturn(SynchronizationContext context)
        {
            SynchronizationContext.SetSynchronizationContext(context);
            return await Task.Run(() => 10).ConfigureAwait(true);
        }

#if NETCOREAPP2_0
        public async Task AsyncMethod_WithValueTask(SynchronizationContext context)
        {
            SynchronizationContext.SetSynchronizationContext(context);
            await new ValueTask(Task.Delay(10)).ConfigureAwait(true);
        }

        public async Task<int> AsyncMethodWithReturn_WithValueTask(SynchronizationContext context)
        {
            SynchronizationContext.SetSynchronizationContext(context);
            await new ValueTask(Task.Delay(10)).ConfigureAwait(true);
            return 10;
        }

        public async Task AsyncGenericMethod_WithValueTask(SynchronizationContext context)
        {
            SynchronizationContext.SetSynchronizationContext(context);
            await new ValueTask(Task.Run(() => 10)).ConfigureAwait(true);
        }

        public async Task<int> AsyncGenericMethodWithReturn_WithValueTask(SynchronizationContext context)
        {
            SynchronizationContext.SetSynchronizationContext(context);
            return await new ValueTask<int>(Task.Run(() => 10)).ConfigureAwait(true);
        }
#endif
    }
}