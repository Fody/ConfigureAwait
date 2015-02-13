using System;
using System.Linq;
using System.Threading.Tasks;
using ConfigureAwait;

namespace AssemblyToProcess
{
    [ConfigureAwait(false)]
    public class Example
    {
        public async Task WithoutConfigureAwait()
        {
            await Task.Delay(10);
        }

        public async Task WithConfigureAwait()
        {
            await Task.Delay(10).ConfigureAwait(false);
        }
    }

    [ConfigureAwait(false)]
    public class ClassWithAttribute
    {
        public async Task AsyncMethod()
        {
            await Task.Delay(10);
        }
    }

    public class MethodWithAttribute
    {
        [ConfigureAwait(false)]
        public async Task AsyncMethod()
        {
            await Task.Delay(10);
        }
    }

    public class AttributeAppliedToNormalMethod
    {
        [ConfigureAwait(false)]
        public void NormalMethod()
        {
        }
    }
}