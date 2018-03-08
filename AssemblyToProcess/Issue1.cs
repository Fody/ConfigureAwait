using System.IO;
using System.Threading.Tasks;
using Fody;

namespace AssemblyToProcess
{
    class Issue1
    {
        [ConfigureAwait(false)]
        async Task WithReaderAndWriter(TextWriter writer, StreamReader reader)
        {
            string line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                await writer.WriteLineAsync(line);
            }
        }
    }
}