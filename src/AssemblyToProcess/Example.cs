using System;
using System.Linq;
using System.Threading.Tasks;
using ConfigureAwait;

namespace AssemblyToProcess
{
    [ConfigureAwait(false)]
    public class Example
    {
        public async Task AsyncMethod()
        {
            await Task.Delay(0);
        }

        public async Task<int> AsyncMethodWithReturn()
        {
            await Task.Delay(0);
            return 10;
        }

        public async Task AsyncGenericMethod()
        {
            await Task.FromResult(0);
        }

        public async Task<int> AsyncGenericMethodWithReturn()
        {
            return await Task.FromResult(10);
        }
    }
}