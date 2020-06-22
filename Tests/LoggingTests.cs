using System.Linq;
using System.Threading.Tasks;
using VerifyXunit;
using Xunit;

public partial class ModuleWeaverTests
{
    [Fact]
    public Task InfoMessages()
    {
        return Verifier.Verify(testResult.Messages.OrderBy(s => s).Select(x=>x.Text));
    }

    [Fact]
    public Task ErrorMessages()
    {
        return Verifier.Verify(testResult.Errors.OrderBy(s => s).Select(x => x.Text));
    }
}