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