using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConfigureAwait;

namespace AssemblyToProcess
{
    [ConfigureAwait(false)]
    public class ClassWithAttribute
    {
        public async Task AsyncMethod(SynchronizationContext context)
        {
            SynchronizationContext.SetSynchronizationContext(context);
            await Task.Delay(0);
        }

        public async Task<int> AsyncMethodWithReturn(SynchronizationContext context)
        {
            SynchronizationContext.SetSynchronizationContext(context);
            await Task.Delay(0);
            return 10;
        }

        public async Task AsyncGenericMethod(SynchronizationContext context)
        {
            SynchronizationContext.SetSynchronizationContext(context);
            await Task.FromResult(0);
        }

        public async Task<int> AsyncGenericMethodWithReturn(SynchronizationContext context)
        {
            SynchronizationContext.SetSynchronizationContext(context);
            return await Task.FromResult(10);
        }
    }
}