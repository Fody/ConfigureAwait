using System;
using System.Threading.Tasks;
using NUnit.Framework;

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
    public async Task AsyncMethodWithReturn()
    {
        var context = (dynamic)Activator.CreateInstance(contextType);
        var test = (dynamic)Activator.CreateInstance(classType);

        Assert.IsFalse(context.Flag);

        var result = await test.AsyncMethodWithReturn(context);

        Assert.IsTrue(context.Flag);
        Assert.AreEqual(10, result);
    }

    [Test]
    public async Task AsyncGenericMethod()
    {
        var context = (dynamic)Activator.CreateInstance(contextType);
        var test = (dynamic)Activator.CreateInstance(classType);

        Assert.IsFalse(context.Flag);

        await test.AsyncGenericMethod(context);

        Assert.IsTrue(context.Flag);
    }

    [Test]
    public async Task AsyncGenericMethodWithReturn()
    {
        var context = (dynamic)Activator.CreateInstance(contextType);
        var test = (dynamic)Activator.CreateInstance(classType);

        Assert.IsFalse(context.Flag);

        var result = await test.AsyncGenericMethodWithReturn(context);

        Assert.IsTrue(context.Flag);
        Assert.AreEqual(10, result);
    }
}