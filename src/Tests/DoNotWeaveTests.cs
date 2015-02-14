using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Tests.Helpers;

namespace Tests
{
    [TestFixture]
    public class DoNotWeaveTests
    {
        private Type contextType;
        private Type classType;

        [SetUp]
        public void SetUp()
        {
            contextType = AssemblyWeaver.Assembly.GetType("AssemblyToProcess.FlagSyncronizationContext");
            classType = AssemblyWeaver.Assembly.GetType("AssemblyToProcess.DoNotWeave");
        }

        [Test]
        public async Task AsyncMethod()
        {
            var context = (dynamic)Activator.CreateInstance(contextType);
            var test = (dynamic)Activator.CreateInstance(classType);

            Assert.IsFalse(context.Flag);

            await test.AsyncMethod(context);

            Assert.IsTrue(context.Flag);
        }

        [Test]
        public async void AsyncMethodWithReturn()
        {
            var context = (dynamic)Activator.CreateInstance(contextType);
            var test = (dynamic)Activator.CreateInstance(classType);

            Assert.IsFalse(context.Flag);

            var result = await test.AsyncMethodWithReturn(context);

            Assert.IsTrue(context.Flag);
            Assert.AreEqual(10, result);
        }

        [Test]
        public async void AsyncGenericMethod()
        {
            var context = (dynamic)Activator.CreateInstance(contextType);
            var test = (dynamic)Activator.CreateInstance(classType);

            Assert.IsFalse(context.Flag);

            await test.AsyncGenericMethod(context);

            Assert.IsTrue(context.Flag);
        }

        [Test]
        public async void AsyncGenericMethodWithReturn()
        {
            var context = (dynamic)Activator.CreateInstance(contextType);
            var test = (dynamic)Activator.CreateInstance(classType);

            Assert.IsFalse(context.Flag);

            var result = await test.AsyncGenericMethodWithReturn(context);

            Assert.IsTrue(context.Flag);
            Assert.AreEqual(10, result);
        }
    }
}