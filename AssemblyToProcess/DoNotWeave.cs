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
    }
}