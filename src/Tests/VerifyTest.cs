using System;
using System.Linq;
using NUnit.Framework;
using Tests.Helpers;

namespace Tests
{
    [TestFixture]
    public class VerifyTest
    {
        [Test]
        public void PeVerify()
        {
            Verifier.Verify(AssemblyWeaver.BeforeAssemblyPath, AssemblyWeaver.AfterAssemblyPath);
        }
    }
}