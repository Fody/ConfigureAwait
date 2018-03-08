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
    }

    sealed class GenericMethod
    {
        [ConfigureAwait(false)]
        public async Task Method<TItem>(Task<TItem> itemTask)
        {
            var item = await itemTask;
        }
    }
}