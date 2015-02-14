![Icon](https://raw.github.com/distantcam/ConfigureAwait/master/img/project_icon.png)

### This is an add-in for [Fody](https://github.com/Fody/Fody/) 

Allows you to set your async code's [`ConfigureAwait`](https://msdn.microsoft.com/en-us/library/system.threading.tasks.task.configureawait) at a global level.

### Nuget package [![NuGet Status](http://img.shields.io/nuget/v/ConfigureAwait.Fody.svg?style=flat-square)](https://www.nuget.org/packages/ConfigureAwait.Fody/) [![Build Status](https://img.shields.io/appveyor/ci/distantcam/ConfigureAwait.svg?style=flat-square)](https://ci.appveyor.com/project/distantcam/configureawait) ![.NET 4.0](https://img.shields.io/badge/.NET-4.0-blue.svg?style=flat-square) ![.NET 4.5](https://img.shields.io/badge/.NET-4.5-blue.svg?style=flat-square)

Available here http://nuget.org/packages/ConfigureAwait.Fody 

To Install from the Nuget Package Manager Console 
    
    PM> Install-Package ConfigureAwait.Fody

## Example

### Your code

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

### What gets compiled

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

## Icon

Created by Dmitry Baranovskiy from the Noun Project.