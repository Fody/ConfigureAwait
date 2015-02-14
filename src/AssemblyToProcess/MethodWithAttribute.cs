using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConfigureAwait;

namespace AssemblyToProcess
{
    public class MethodWithAttribute
    {
        [ConfigureAwait(false)]
        public async Task AsyncMethod(SynchronizationContext context)
        {
            SynchronizationContext.SetSynchronizationContext(context);
            await Task.Delay(0);
        }
    }
}