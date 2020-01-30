# <img src="/package_icon.png" height="30px"> ConfigureAwait.Fody

[![Chat on Gitter](https://img.shields.io/gitter/room/fody/fody.svg)](https://gitter.im/Fody/Fody)
[![NuGet Status](https://badge.fury.io/nu/configureawait.fody.svg)](https://www.nuget.org/packages/ConfigureAwait.Fody/)

Configure async code's [`ConfigureAwait`](https://msdn.microsoft.com/en-us/library/system.threading.tasks.task.configureawait) at a global level.


### This is an add-in for [Fody](https://github.com/Fody/Home/)

**It is expected that all developers using Fody either [become a Patron on OpenCollective](https://opencollective.com/fody/), or have a [Tidelift Subscription](https://tidelift.com/subscription/pkg/nuget-fody?utm_source=nuget-fody&utm_medium=referral&utm_campaign=enterprise). [See Licensing/Patron FAQ](https://github.com/Fody/Home/blob/master/pages/licensing-patron-faq.md) for more information.**


## Usage

See also [Fody usage](https://github.com/Fody/Home/blob/master/pages/usage.md).


### NuGet package

Install the [ConfigureAwait.Fody NuGet package](https://nuget.org/packages/ConfigureAwait.Fody/) and update the [Fody NuGet package](https://nuget.org/packages/Fody/):

```
PM> Install-Package Fody
PM> Install-Package ConfigureAwait.Fody
```

The `Install-Package Fody` is required since NuGet always defaults to the oldest, and most buggy, version of any dependency.


### How to use it

By default, `ConfigureAwait.Fody` doesn't change any code. Set a configure await value at the assembly, class, or method level.

 * `[assembly: Fody.ConfigureAwait(false)]` - Assembly level
 * `[Fody.ConfigureAwait(false)]` - Class or method level


### Add to FodyWeavers.xml

Add `<ConfigureAwait/>` to [FodyWeavers.xml](https://github.com/Fody/Home/blob/master/pages/usage.md#add-fodyweaversxml)

```xml
<Weavers>
  <ConfigureAwait/>
</Weavers>
```

It is also possible set the default ContinueOnCapturedContext in the xml config:

```xml
<Weavers>
  <ConfigureAwait ContinueOnCapturedContext="false" />
</Weavers>
```


## Example


### Before code

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

Created by Dmitry Baranovskiy from [The Noun Project](https://thenounproject.com).