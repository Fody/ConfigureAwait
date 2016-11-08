using System;
using System.Threading.Tasks;
using NUnit.Framework;

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
    public async Task AsyncMethod()
    {
        var context = (dynamic)Activator.CreateInstance(contextType);
        var test = (dynamic)Activator.CreateInstance(classType);

        Assert.IsFalse(context.Flag);

        await test.AsyncMethod(context);

        Assert.IsFalse(context.Flag);
    }

    [Test]
    public async Task AsyncMethodWithReturn()
    {
        var context = (dynamic)Activator.CreateInstance(contextType);
        var test = (dynamic)Activator.CreateInstance(classType);

        Assert.IsFalse(context.Flag);

        var result = await test.AsyncMethodWithReturn(context);

        Assert.IsFalse(context.Flag);
        Assert.AreEqual(10, result);
    }

    [Test]
    public async Task AsyncGenericMethod()
    {
        var context = (dynamic)Activator.CreateInstance(contextType);
        var test = (dynamic)Activator.CreateInstance(classType);

        Assert.IsFalse(context.Flag);

        await test.AsyncGenericMethod(context);

        Assert.IsFalse(context.Flag);
    }

    [Test]
    public async Task AsyncGenericMethodWithReturn()
    {
        var context = (dynamic)Activator.CreateInstance(contextType);
        var test = (dynamic)Activator.CreateInstance(classType);

        Assert.IsFalse(context.Flag);

        var result = await test.AsyncGenericMethodWithReturn(context);

        Assert.IsFalse(context.Flag);
        Assert.AreEqual(10, result);
    }
}