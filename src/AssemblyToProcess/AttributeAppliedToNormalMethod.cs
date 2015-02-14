using System;
using System.Linq;
using Fody;

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