using System.Runtime.CompilerServices;
using ApprovalTests;
using ApprovalTests.Namers;
using NUnit.Framework;

[TestFixture, Category("ILSpecific")]
[UseApprovalSubdirectory("ApprovalFiles")]
public class VerifyTest
{
    [SetUp]
    public void SetApprovalConfig()
    {
#if DEBUG
        ApprovalTests.Namers.NamerFactory.AsEnvironmentSpecificTest(() => "Debug");
#else
        ApprovalTests.Namers.NamerFactory.AsEnvironmentSpecificTest(() => "Release");
#endif
    }

    [Test]
    public void PeVerify()
    {
        Verifier.Verify(AssemblyWeaver.BeforeAssemblyPath, AssemblyWeaver.AfterAssemblyPath);
    }

    [Test]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void DecompileExample()
    {
        Approvals.Verify(Decompiler.Decompile(AssemblyWeaver.AfterAssemblyPath, "AssemblyToProcess.Example"));
    }

    [Test]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void DecompileIssue1()
    {
        Approvals.Verify(Decompiler.Decompile(AssemblyWeaver.AfterAssemblyPath, "AssemblyToProcess.Issue1"));
    }

    [Test]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void DecompileGenericClass()
    {
        Approvals.Verify(Decompiler.Decompile(AssemblyWeaver.AfterAssemblyPath, "AssemblyToProcess.GenericClass`1"));
    }

    [Test]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void DecompileGenericMethod()
    {
        Approvals.Verify(Decompiler.Decompile(AssemblyWeaver.AfterAssemblyPath, "AssemblyToProcess.GenericMethod"));
    }

    [Test]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void DecompileCatchAndFinally()
    {
        Approvals.Verify(Decompiler.Decompile(AssemblyWeaver.AfterAssemblyPath, "AssemblyToProcess.CatchAndFinally"));
    }
}