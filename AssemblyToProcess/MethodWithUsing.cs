namespace AssemblyToProcess
{
    using System;
    using System.Threading.Tasks;
    using Fody;

    public class MethodWithUsing
    {
        [ConfigureAwait(false)]
        public async Task AsyncMethod()
        {
            using(await NewMethod()){}
        }

        static async Task<IDisposable> NewMethod()
        {
            await Task.Delay(0);
            return new MyDisposable();
        }

#if NETCOREAPP2_0
        [ConfigureAwait(false)]
        public async Task AsyncMethod_WithValueTask()
        {
            using(await new ValueTask<IDisposable>(NewMethod_WithValueTask())){}
        }

        static async Task<IDisposable> NewMethod_WithValueTask()
        {
            await new ValueTask(Task.Delay(0));
            return new MyDisposable();
        }
#endif
    }

    public class MyDisposable : IDisposable
    {
        public void Dispose()
        {
            Disposed = true;
        }

        public static bool Disposed;
    }
}