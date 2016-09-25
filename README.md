# YodiiStaticProxy.Fody

## What is this?

**YodiiStaticProxy.Fody** is an add-in for [Fody](https://github.com/Fody/Fody). It is made to be used by [Yodii](https://github.com/Invenietis/yodii) services in projects using Yodii as a DI container or in other ways in Yodii-based projects.

## Why? 

Generating proxies at compile time using YodiiStaticProxy.Fody allows Yodii-based projects to run on WinRT and iOS platforms (which do not support `System.Reflection.Emit`). Also potentially improves runtime performance since work is done beforehand.

## How to use

Once you added the dependency to your assembly containing your Yodii services, just press build. To be picked up by YodiiStaticProxy.Fody, each of your YodiiService **must** be tagged with the IYodiiService marker interface.
- The generated assembly will bear the name "*NameOfAssemblyBeingWeaved.YodiiStaticProxy.dll*" and be located at the root of your solution in a /lib folder.
- Foreach Yodii service picked up by YodiiStaticProxy.Fody, this generated assembly will hold 2 generated proxy types (the regular proxy and the unavailable proxy type used by Yodii when the service is unavailable for various reasons), along with the pdb file.

### Thanks
- [@olivier-spinelli](https://github.com/olivier-spinelli) For Yodii
- [@BrunoJuchli](https://github.com/BrunoJuchli) For his help and remarks building this. Check out [StaticProxy](https://github.com/BrunoJuchli/StaticProxy.Fody)
