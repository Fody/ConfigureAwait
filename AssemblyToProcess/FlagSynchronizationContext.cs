using System.Threading;

namespace AssemblyToProcess
{
    public class FlagSynchronizationContext : SynchronizationContext
    {
        public bool Flag { get; set; }

        public override void Post(SendOrPostCallback d, object state)
        {
            Flag = true;
            base.Post(d, state);
        }
    }
}