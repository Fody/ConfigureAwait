using System;
using System.Linq;
using System.Threading.Tasks;

namespace AssemblyToProcess
{
    public class Example
    {
        public async Task WithoutConfigureAwait()
        {
            await Task.Delay(10);
            await Task.Delay(10);
            await Task.Delay(10);
        }

        public async Task WithConfigureAwait()
        {
            await Task.Delay(10).ConfigureAwait(false);
            await Task.Delay(10).ConfigureAwait(false);
            await Task.Delay(10).ConfigureAwait(false);
        }
    }
}