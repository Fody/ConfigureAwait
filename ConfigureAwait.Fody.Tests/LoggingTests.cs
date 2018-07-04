#if NET46
using System.Linq;
using System.Runtime.CompilerServices;
using ApprovalTests;
using Xunit;

public partial class ModuleWeaverTests
{
    [Fact]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void InfoMessages()
    {
        Approvals.VerifyAll(testResult.Messages.OrderBy(s => s).Select(x=>x.Text), "Info");
    }

    [Fact]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void WarningMessages()
    {
        Approvals.VerifyAll(testResult.Warnings.OrderBy(s => s).Select(x => x.Text), "Warning");
    }

    [Fact]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void ErrorMessages()
    {
        Approvals.VerifyAll(testResult.Errors.OrderBy(s => s).Select(x => x.Text), "Error");
    }
}
#endif