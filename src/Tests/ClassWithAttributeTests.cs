using System;
using System.Linq;
using NUnit.Framework;
using Tests.Helpers;

namespace Tests
{
    [TestFixture]
    public class ClassWithAttributeTests
    {
        private Type contextType;
        private Type classType;

        [SetUp]
        public void SetUp()
        {
            contextType = AssemblyWeaver.Assembly.GetType("AssemblyToProcess.FlagSyncronizationContext");
            classType = AssemblyWeaver.Assembly.GetType("AssemblyToProcess.ClassWithAttribute");
        }

        [Test]
        public async void AsyncMethod()
        {
            var context = (dynamic)Activator.CreateInstance(contextType);
            var test = (dynamic)Activator.CreateInstance(classType);

            Assert.IsFalse(context.Flag);

            await test.AsyncMethod(context);

            Assert.IsFalse(context.Flag);
        }

        [Test]
        public async void AsyncMethodWithReturn()
        {
            var context = (dynamic)Activator.CreateInstance(contextType);
            var test = (dynamic)Activator.CreateInstance(classType);

            Assert.IsFalse(context.Flag);

            var result = await test.AsyncMethodWithReturn(context);

            Assert.IsFalse(context.Flag);
            Assert.AreEqual(10, result);
        }

        [Test]
        public async void AsyncGenericMethod()
        {
            var context = (dynamic)Activator.CreateInstance(contextType);
            var test = (dynamic)Activator.CreateInstance(classType);

            Assert.IsFalse(context.Flag);

            await test.AsyncGenericMethod(context);

            Assert.IsFalse(context.Flag);
        }

        [Test]
        public async void AsyncGenericMethodWithReturn()
        {
            var context = (dynamic)Activator.CreateInstance(contextType);
            var test = (dynamic)Activator.CreateInstance(classType);

            Assert.IsFalse(context.Flag);

            var result = await test.AsyncGenericMethodWithReturn(context);

            Assert.IsFalse(context.Flag);
            Assert.AreEqual(10, result);
        }
    }
}