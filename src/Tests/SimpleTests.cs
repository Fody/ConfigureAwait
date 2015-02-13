using System;
using System.Linq;
using System.Runtime.CompilerServices;
using ApprovalTests;
using ApprovalTests.Namers;
using NUnit.Framework;
using Tests.Helpers;

namespace Tests
{
    [TestFixture]
    [UseApprovalSubdirectory("ApprovalFiles")]
    public class SimpleTests
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
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Example()
        {
            Approvals.Verify(Decompiler.Decompile(AssemblyWeaver.AfterAssemblyPath, "AssemblyToProcess.Example"));
        }
    }
}