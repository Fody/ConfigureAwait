using System.Threading.Tasks;
using Fody;

namespace AssemblyToProcess
{
    internal sealed class GenericClass<TItem>
    {
        [ConfigureAwait(false)]
        public async Task Method(Task<TItem> itemTask)
        {
            var item = await itemTask;
        }
    }

    internal sealed class GenericMethod
    {
        [ConfigureAwait(false)]
        public async Task Method<TItem>(Task<TItem> itemTask)
        {
            var item = await itemTask;
        }
    }
}