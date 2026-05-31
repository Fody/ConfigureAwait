using Fody;

public class AwaitTernary
{
    [ConfigureAwait(false)]
    public async Task Method(string value) =>
        await (value == "anyValue" ? Task.Delay(1) : Task.Delay(2));
}
