![Icon](https://raw.github.com/Fody/ConfigureAwait/master/project_icon.png)


### This is an add-in for [Fody](https://github.com/Fody/Fody/) 

Allows you to set your async code's [`ConfigureAwait`](https://msdn.microsoft.com/en-us/library/system.threading.tasks.task.configureawait) at a global level.

### Nuget package [![NuGet Status](http://img.shields.io/nuget/v/ConfigureAwait.Fody.svg?style=flat-square)](https://www.nuget.org/packages/ConfigureAwait.Fody/) [![Build Status](https://img.shields.io/appveyor/ci/distantcam/ConfigureAwait.svg?style=flat-square)](https://ci.appveyor.com/project/distantcam/configureawait)

Available here http://nuget.org/packages/ConfigureAwait.Fody 

To Install from the NuGet Package Manager Console 
    
```
PM> Install-Package ConfigureAwait.Fody
```

## How to use it

By default, `ConfigureAwait.Fody` doesn't change any of your code. You have to explicitly set a configure await value at the assembly, class, or method level.

 * `[assembly: Fody.ConfigureAwait(false)]` - Assembly level
 * `[Fody.ConfigureAwait(false)]` - Class or method level


### Add to FodyWeavers.xml

Add `<ConfigureAwait/>` to [FodyWeavers.xml](https://github.com/Fody/Fody#add-fodyweaversxml)

```xml
<?xml version="1.0" encoding="utf-8" ?>
<Weavers>
  <ConfigureAwait/>
</Weavers>
```

## Example


### Your code

```
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

```
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