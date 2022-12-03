using Fody;

class Issue1
{
    [ConfigureAwait(false)]
    async Task WithReaderAndWriter(TextWriter writer, StreamReader reader)
    {
        while (await reader.ReadLineAsync() is { } line)
        {
            await writer.WriteLineAsync(line);
        }
    }

#if NETCOREAPP2_0
    [ConfigureAwait(false)]
    async Task WithReaderAndWriter_WithValueTask(TextWriter writer, StreamReader reader)
    {
        while (await new ValueTask<string>(reader.ReadLineAsync()) is { } line)
        {
            await new ValueTask(writer.WriteLineAsync(line));
        }
    }
#endif
}