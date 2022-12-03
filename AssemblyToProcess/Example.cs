using Fody;

public class Example
{
    public async Task AsyncMethod1()
    {
        await Task.Delay(1);
    }

    [ConfigureAwait(false)]
    public async Task AsyncMethod2()
    {
        await Task.Delay(1);
    }

    public async Task AsyncMethod3()
    {
        await Task.Delay(1).ConfigureAwait(false);
    }

    public async Task<int> AsyncMethod4()
    {
        var result = await Task.FromResult(10);
        return result;
    }

    [ConfigureAwait(false)]
    public async Task<int> AsyncMethod5()
    {
        var result = await Task.FromResult(10);
        return result;
    }

    public async Task<int> AsyncMethod6()
    {
        var result = await Task.FromResult(10).ConfigureAwait(false);
        return result;
    }

    public async Task<int> AsyncMethod7()
    {
        var count = await Task.FromResult(10);
        var sum = 0;
        for (var i = 0; i < count; i++)
        {
            await Task.Delay(1);
            sum += await Task.FromResult(i);
        }
        return sum;
    }

    [ConfigureAwait(false)]
    public async Task<int> AsyncMethod8()
    {
        var count = await Task.FromResult(10);
        var sum = 0;
        for (var i = 0; i < count; i++)
        {
            await Task.Delay(1);
            sum += await Task.FromResult(i);
        }
        return sum;
    }

    public async Task<int> AsyncMethod9()
    {
        var count = await Task.FromResult(10).ConfigureAwait(false);
        var sum = 0;
        for (var i = 0; i < count; i++)
        {
            await Task.Delay(1).ConfigureAwait(false);
            sum += await Task.FromResult(i).ConfigureAwait(false);
        }
        return sum;
    }

    public async Task<Example> AsyncMethod10()
    {
        var result = await Task.FromResult(new Example());
        return result;
    }

    [ConfigureAwait(false)]
    public async Task<Example> AsyncMethod11()
    {
        var result = await Task.FromResult(new Example());
        return result;
    }

    public async Task<Example> AsyncMethod12()
    {
        var result = await Task.FromResult(new Example()).ConfigureAwait(false);
        return result;
    }

#if NETCOREAPP2_0
    public async Task AsyncMethod1_WithValueTask()
    {
        await new ValueTask(Task.Delay(1));
    }

    [ConfigureAwait(false)]
    public async Task AsyncMethod2_WithValueTask()
    {
        await new ValueTask(Task.Delay(1));
    }

    public async Task AsyncMethod3_WithValueTask()
    {
        await new ValueTask(Task.Delay(1)).ConfigureAwait(false);
    }

    public async Task<int> AsyncMethod4_WithValueTask()
    {
        var result = await new ValueTask<int>(Task.FromResult(10));
        return result;
    }

    [ConfigureAwait(false)]
    public async Task<int> AsyncMethod5_WithValueTask()
    {
        var result = await new ValueTask<int>(Task.FromResult(10));
        return result;
    }

    public async Task<int> AsyncMethod6_WithValueTask()
    {
        var result = await new ValueTask<int>(Task.FromResult(10)).ConfigureAwait(false);
        return result;
    }

    public async Task<int> AsyncMethod7_WithValueTask()
    {
        var count = await new ValueTask<int>(Task.FromResult(10));
        var sum = 0;
        for (var i = 0; i < count; i++)
        {
            await Task.Delay(1);
            sum += await Task.FromResult(i);
        }
        return sum;
    }

    [ConfigureAwait(false)]
    public async Task<int> AsyncMethod8_WithValueTask()
    {
        var count = await new ValueTask<int>(Task.FromResult(10));
        var sum = 0;
        for (var i = 0; i < count; i++)
        {
            await new ValueTask(Task.Delay(1));
            sum += await new ValueTask<int>(Task.FromResult(i));
        }
        return sum;
    }

    public async Task<int> AsyncMethod9_WithValueTask()
    {
        var count = await new ValueTask<int>(Task.FromResult(10)).ConfigureAwait(false);
        var sum = 0;
        for (var i = 0; i < count; i++)
        {
            await new ValueTask(Task.Delay(1)).ConfigureAwait(false);
            sum += await new ValueTask<int>(Task.FromResult(i)).ConfigureAwait(false);
        }
        return sum;
    }

    public async Task<Example> AsyncMethod10_WithValueTask()
    {
        var result = await new ValueTask<Example>(Task.FromResult(new Example()));
        return result;
    }

    [ConfigureAwait(false)]
    public async Task<Example> AsyncMethod11_WithValueTask()
    {
        var result = await new ValueTask<Example>(Task.FromResult(new Example()));
        return result;
    }

    public async Task<Example> AsyncMethod12_WithValueTask()
    {
        var result = await new ValueTask<Example>(Task.FromResult(new Example())).ConfigureAwait(false);
        return result;
    }
#endif
}
