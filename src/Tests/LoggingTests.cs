using System.Linq;
using System.Runtime.CompilerServices;
using ApprovalTests.Namers;
using ApprovalTests.Reporters;
using NUnit.Framework;

[TestFixture]
[UseApprovalSubdirectory("ApprovalFiles")]
[UseReporter(typeof(DiffReporter))]
public class LoggingTests
{
    [Test]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void InfoMessages()
    {
        ApprovalTests.Approvals.VerifyAll(AssemblyWeaver.Infos.OrderBy(s => s), "Info");
    }

    [Test]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void WarningMessages()
    {
        ApprovalTests.Approvals.VerifyAll(AssemblyWeaver.Warnings.OrderBy(s => s), "Warning");
    }

    [Test]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void ErrorMessages()
    {
        ApprovalTests.Approvals.VerifyAll(AssemblyWeaver.Errors.OrderBy(s => s), "Error");
    }
}