using System;
using System.Linq;
using ConfigureAwait;

namespace AssemblyToProcess
{
    public class AttributeAppliedToNormalMethod
    {
        [ConfigureAwait(false)]
        public void NormalMethod()
        {
        }
    }
}