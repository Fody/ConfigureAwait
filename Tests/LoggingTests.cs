using System.Linq;
using System.Threading.Tasks;
using Xunit;

public partial class ModuleWeaverTests
{
    [Fact]
    public Task InfoMessages()
    {
        return Verify(testResult.Messages.OrderBy(s => s).Select(x=>x.Text));
    }

    [Fact]
    public Task ErrorMessages()
    {
        return Verify(testResult.Errors.OrderBy(s => s).Select(x => x.Text));
    }
}