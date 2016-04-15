using System;
using System.Threading.Tasks;
using Fody;

namespace AssemblyToProcess
{
    public class CatchAndFinally
    {
        public async Task Catch1()
        {
            try
            {
                throw new NotImplementedException();
            }
            catch
            {
                await Task.Delay(1);
            }
        }

        [ConfigureAwait(false)]
        public async Task Catch2()
        {
            try
            {
                throw new NotImplementedException();
            }
            catch
            {
                await Task.Delay(1);
            }
        }

        public async Task Catch3()
        {
            try
            {
                throw new NotImplementedException();
            }
            catch
            {
                await Task.Delay(1).ConfigureAwait(false);
            }
        }

        public async Task Finally1()
        {
            try
            {
                throw new NotImplementedException();
            }
            finally
            {
                await Task.Delay(1);
            }
        }

        [ConfigureAwait(false)]
        public async Task Finally2()
        {
            try
            {
                throw new NotImplementedException();
            }
            finally
            {
                await Task.Delay(1);
            }
        }

        public async Task Finally3()
        {
            try
            {
                throw new NotImplementedException();
            }
            finally
            {
                await Task.Delay(1).ConfigureAwait(false);
            }
        }
    }
}