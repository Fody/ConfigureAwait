using System;
using System.Linq;
using ApprovalTests.Reporters;
using NUnit.Framework;
using Tests.Helpers;

namespace Tests
{
    [TestFixture]
    [UseReporter(typeof(DiffReporter))]
    public class VerifyTest
    {
        [Test]
        public void PeVerify()
        {
            Verifier.Verify(AssemblyWeaver.BeforeAssemblyPath, AssemblyWeaver.AfterAssemblyPath);
        }
    }
}