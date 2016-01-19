using System.IO;
using System.Threading.Tasks;
using Fody;
using System;
using System.Collections.Generic;

namespace AssemblyToProcess
{
    internal sealed class GenericIssue<TElement, TKey>
    {

        private readonly Func<TElement, Task<TKey>> keySelector;

        [ConfigureAwait(false)]
        internal async Task Initialize(TElement[] elements)
        {
            await keySelector(default(TElement));//.ConfigureAwait(false);
        }
    }
}