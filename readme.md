![Icon](https://raw.github.com/Fody/ConfigureAwait/master/package_icon.png)


### This is an add-in for [Fody](https://github.com/Fody/Home/)

Allows you to set your async code's [`ConfigureAwait`](https://msdn.microsoft.com/en-us/library/system.threading.tasks.task.configureawait) at a global level.


### NuGet package

Install the [ConfigureAwait.Fody NuGet package](https://nuget.org/packages/ConfigureAwait.Fody/) and update the [Fody NuGet package](https://nuget.org/packages/Fody/):

```
PM> Install-Package Fody
PM> Install-Package ConfigureAwait.Fody
```

The `Install-Package Fody` is required since NuGet always defaults to the oldest, and most buggy, version of any dependency.


## How to use it

By default, `ConfigureAwait.Fody` doesn't change any of your code. You have to explicitly set a configure await value at the assembly, class, or method level.

 * `[assembly: Fody.ConfigureAwait(false)]` - Assembly level
 * `[Fody.ConfigureAwait(false)]` - Class or method level


### Add to FodyWeavers.xml

Add `<ConfigureAwait/>` to [FodyWeavers.xml](https://github.com/Fody/Home/blob/master/pages/usage.md#add-fodyweaversxml)

```xml
<?xml version="1.0" encoding="utf-8" ?>
<Weavers>
  <ConfigureAwait/>
</Weavers>
```


## Example


### Your code

```csharp
using Fody;

[ConfigureAwait(false)]
public class MyAsyncLibrary
{
    public async Task MyMethodAsync()
    {
        await Task.Delay(10);
        await Task.Delay(20);
    }

    public async Task AnotherMethodAsync()
    {
        await Task.Delay(30);
    }
}
```


### What gets compiled

```csharp
public class MyAsyncLibrary
{
    public async Task MyMethodAsync()
    {
        await Task.Delay(10).ConfigureAwait(false);
        await Task.Delay(20).ConfigureAwait(false);
    }

    public async Task AnotherMethodAsync()
    {
        await Task.Delay(30).ConfigureAwait(false);
    }
}
```


## Icon

Created by Dmitry Baranovskiy from the Noun Project.
