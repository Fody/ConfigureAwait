using VerifyTUnit;
public partial class ModuleWeaverTests
{
    [Test]
    public async Task InfoMessages()
    {
        await Verifier.Verify(testResult.Messages.OrderBy(s => s).Select(x=>x.Text));
    }

    [Test]
    public async Task ErrorMessages()
    {
        await Verifier.Verify(testResult.Errors.OrderBy(s => s).Select(_ => _.Text));
    }
}