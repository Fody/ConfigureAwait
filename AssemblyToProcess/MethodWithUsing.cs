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