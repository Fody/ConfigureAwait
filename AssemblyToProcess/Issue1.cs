using System.IO;
using System.Threading.Tasks;
using Fody;

class Issue1
{
    [ConfigureAwait(false)]
    async Task WithReaderAndWriter(TextWriter writer, StreamReader reader)
    {
        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            await writer.WriteLineAsync(line);
        }
    }

#if NETCOREAPP2_0
    [ConfigureAwait(false)]
    async Task WithReaderAndWriter_WithValueTask(TextWriter writer, StreamReader reader)
    {
        string line;
        while ((line = await new ValueTask<string>(reader.ReadLineAsync())) != null)
        {
            await new ValueTask(writer.WriteLineAsync(line));
        }
    }
#endif
}