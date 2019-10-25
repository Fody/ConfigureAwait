using System.Threading.Tasks;
using Fody;
// ReSharper disable UnusedVariable

namespace AssemblyToProcess
{
    sealed class GenericClass<TItem>
    {
        [ConfigureAwait(false)]
        public async Task Method(Task<TItem> itemTask)
        {
            var item = await itemTask;
        }

#if NETCOREAPP2_0
        [ConfigureAwait(false)]
        public async Task Method_WithValueTask(Task<TItem> itemTask)
        {
            var item = await new ValueTask<TItem>(itemTask);
        }
#endif
    }

    sealed class GenericMethod
    {
        [ConfigureAwait(false)]
        public async Task Method<TItem>(Task<TItem> itemTask)
        {
            var item = await itemTask;
        }

#if NETCOREAPP2_0
        [ConfigureAwait(false)]
        public async Task Method_WithValueTask<TItem>(Task<TItem> itemTask)
        {
            var item = await new ValueTask<TItem>(itemTask);
        }
#endif
    }
}