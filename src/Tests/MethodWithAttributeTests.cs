using System;
using NUnit.Framework;

[TestFixture]
public class MethodWithAttributeTests
{
    private Type contextType;
    private Type classType;

    [SetUp]
    public void SetUp()
    {
        contextType = AssemblyWeaver.Assembly.GetType("AssemblyToProcess.FlagSyncronizationContext");
        classType = AssemblyWeaver.Assembly.GetType("AssemblyToProcess.MethodWithAttribute");
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
}