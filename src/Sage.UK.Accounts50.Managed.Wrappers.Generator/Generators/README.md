# Getting started

The `Sage.UK.Accounts50.Managed.Wrappers.Generator.exe` gets invoked when the `Schema` project is built. The generator input is the `Sage.UK.Accounts50.BusinessObjects.Managed.Wrappers\HeadersToParse.h` file and the output goes to `Sage.UK.Accounts50.BusinessObjects.Managed.Wrappers\Generated` folder.

The `Sage.UK.Accounts50.BusinessObjects.Managed.Wrappers` project has three predefined headers:

- `HeadersToParse.h` - This contains includes for every header containing code we want to generate wrappers for.
- `Framekwork.h` - This contains common headers that don't frequently change, and are needed for the code generation because they are not directly included in the header for the code we want to generate (i.e. their types are forward declared). This file is included in `HeadersToParse.h` so that the generator can know about the types being used by the code we tell it to generate wrappers for, it's also included in `StdAfx.h` as most of the generated wrappers depend on some types from those common headers.
- `StdAfx.h` - Includes infrequently changed headers needed by the generated code to compile. This includes `Framework.h`, as well any other headers that don't frequently change, but are not needed by the code generation, unlike the ones that come from `Framework.h`.

# Testing the wrapper code

The `Sage.UK.Accounts50.BusinessObjects.Managed.Wrappers.Tests` can be used to write tests that exercise the generated wrapper code. The project uses the same infrastructure as the existing `BusinessObjectMsTests` project, therefore you can test wrapper code that goes to DTA files, etc.

It is recommended a test is added for any functionality we come to use from the wrapper, to ensure any future breakages can be detected if any of the code generation changes/breaks.

# Debugging CppSharp

We can debug the `CppSharp` code that our generator uses to diagnose code generation issues. To be able to debug `CppSharp`, we need to clone the project's repository from [here](https://github.com/mono/CppSharp). The following command will clone the repository:

```
git clone https://github.com/mono/CppSharp.git
```

We then need to check out the git commit that represents the build we are integrating with. The build commit is available from the URL where we download the build from. Following is the current git commit for the build we are using (keep this section up to date whenever we take a new build):

`fe334888`

To check out the commit type:

```
git checkout <commit Id>
```

Once the right git commit is checked out, we need to generate the Visual Studio files to open the `CppSharp` code. For this we need Visual Studio 2017. Open a Visual Studio 2017 command prompt inside the `CppSharp` repository root folder and type:

```
build\premake5.exe --file=build\scripts\LLVM.lua download_llvm --arch=x86
```

Following that type this command to generate the Visual Studio solution:

```
build\premake5.exe --file=build\premake5.lua vs2017 --arch=x86
```

This will generate a Visual Studio 2017 solution file. We can open it and attach the debugger to our generator exe in order to debug `CppSharp` source. You can also build `CppSharp` itself, however note that `CppSharp` only builds in release under x86 platform.

# FAQ

## Where to include headers?

- If a header is commonly used, doesn't change frequently, and is needed by the code generation, then it should be included in `Framework.h`.
- If a header is commonly used, doesn't change frequently, and is not needed by the code generation, but is needed by the generated code to compile, then it should be included in `StdAfx.h`.
- For anything else, put the include in `HeadersToParse.h` (i.e. headers that are mainly needed for the code generation).

Note that business object headers representing DTA files are automatically included in `HeadersToParse.h`. The generator uses a convention to include any business object headers if it detects there is another file starting with the same name but ending with the suffix **Collection**. This works because we generate a collection header for every DTA business object class.

## Why are there no wrappers generated for some native code?

There are a number of reasons this might happen.

### Adding a new native header

If you add a new header with native code you wish to generate wrappers for, if that header is not already visible through `HeadersToParse.h` (or one of the headers it includes), then it must be made visible to the generator, using one of the suggestions from [Where to include headers?](#where-to-include-headers).

### Adding dependency to existing native header

If you add a new dependency to one of the existing native headers used by the generator (e.g. a new class/struct), then that dependency must be visible to the generator. Best way to ensure that is to put the include of that dependency in the headers where it will be used, rather than forward declaring it. Consider the following example:

`CloudConnectionInfo.h`
```cpp
#include <string>

struct CloudConnectionInfo
{
  std::string userId;
  ...
};
```

`BConnectedCloud.h`
```cpp
#pragma once

#include "BusObject.h"

class BUSOBJS_API CBConnectedCloud : public BBusObject
{
public:
	bool ConnectedCloudRegister(const DatabaseLib::CloudConnectionInfo& info);
  ...
};
```

`BConnectedCloud.h` makes use of `CloudConnectionInfo`, however it does not include its header. The native `BConnectedCloud.cpp` will compile due to the pre-compiled header including the necessary header. However, the generator cannot see that, and therefore won't generate a wrapper for `ConnectedCloudRegister`.

### Adding dependency from an ignored header

The generator explicitly ignores a number of headers either because they were considered unnecessary because they are not used by any of the native code we generate wrappers for, and therefore will increase build time, or they contain non standard code that is not possible for the generator to understand.

As an example, the generator calls `IgnoreHeadersWithName` method passing it regex patterns so that any header from a data service related project is ignored. For example:

```csharp
public void Preprocess(Driver driver, ASTContext ctx)
{
    ctx.IgnoreHeadersWithName(new List<string>
    {
        "DynamoClientLib/",
        "RequestLib/",
        ...
    });
}
```

The above will ignore any headers that come from `DynamoClientLib` or `RequestLib` projects. If for example a couple of headers are actually needed from one of the ignored projects, then we can write the regex so that it continues to ignore all headers from a project, apart form the specific ones we need. For example:

```csharp
public void Preprocess(Driver driver, ASTContext ctx)
{
    ctx.IgnoreHeadersWithName(new List<string>
    {
        "(DynamoClientLib/)((?!Dataset.h).)*$",
        "(RequestLib/)((?!ClientType.h|CloudStatus.h).)*$",
        ...
    });
}
```

The above will ignore everything from `DynamoClientLib` apart from `Dataset.h`, and everything from `RequestLib` apart from `ClientType.h` and `CloudStatus.h`.

## Why are there no wrappers generated for preprocessor directives (i.e. #define)?

`CppSharp` does not support generating wrappers for preprocessor directives due to the complexity they can have (i.e. #define that acts as functions). If the preprocessor directives are simply defines for constants or flags, then we have few options:

- Turn the #define(s) into an enum. The generator is able to generate wrappers for enums, therefore this will be the ideal solution. The feasibility of this solution depends on how significant the use of the #define(s) is throughout the native code.
- Turn the #define(s) into static const variable inside a class. The same as the above, but the constants exist within a class rather than an enum.

If changing the #define(s) to an enum (or static class members) requires significant update to the existing native code, then the next possible solution is to define an enum/static members whose values map to the existing #define(s). For example:

```
#define PROGRAM_INSTANT 0
#define PROGRAM_FINCON 1
#define PROGRAM_CLIENTMANAGER 2

enum ProgramTypes
{
  Instant = PROGRAM_INSTANT,
  Fincon = PROGRAM_FINCON,
  ClientManager = PROGRAM_CLIENTMANAGER
};
```

The above allows the generator to generate a wrapper for the enum, whose values represent the existing flags from the #define(s), without changing existing native code.

## Non-const reference parameters are not behaving as expected?

The generator handles non-const reference parameters for the following types:

- Primitive types
- String type

The generator will create an `out` parameter type in the generated wrapper, and will reassign it within the generated function after the native call is made. For example:

```cpp
void SageUKAccounts50BusinessObjectsManagedWrappers::CBBusContext::IsCompanyBankCloudEnabled([System::Runtime::InteropServices::In, System::Runtime::InteropServices::Out] bool% bankCloudEnabled, [System::Runtime::InteropServices::In, System::Runtime::InteropServices::Out] bool% bankCloudManagerEnabled)
{
    bool __arg0 = bankCloudEnabled;
    bool __arg1 = bankCloudManagerEnabled;
    ((::CBBusContext*)NativePtr)->IsCompanyBankCloudEnabled(__arg0, __arg1);
    bankCloudEnabled = __arg0;
    bankCloudManagerEnabled = __arg1;
}
```

For other types (classes/structs) the generator does not mark the parameter as `out` or reassigns it after the native function call. This is due to the complexity in knowing how much needs to be reassigned (e.g. do pointer members or other complex members need reassignment?), and the fact that not all complex types have copy constructors we can use. Additionally, a lot of the time the parameters don't actually need to be non-const references because they are not actually modified. There are a number of workarounds for this limitation:

- If the parameter does not get modified, make it const.
- If there is only a single modifiable parameter, and the function return type is void, change the function to return the result rather than set it through a non-const parameter.
- If the existing function return type is not void, create an overload of the function that calls the existing one but instead returns the result. This ensures existing native code does not require a change, while still allowing us to call the new overload from .NET.
- If there are multiple modifiable parameters, create a struct that aggregates them and return that as the result.

Note that although the generator is able to handle string types (e.g. `CSGString` and `std::string`), char arrays/pointers parameters are not generated with an `out` flag. The reason is it's not possible to deduce if a char pointer parameter is intended to be modified or not. The ideal solution is to change the native function so that it returns a string type rather than taking a modifiable char pointer. If such change requires a significant update to the existing native code, then we can create a native overload that calls the existing native function with a modifiable char pointer, and returns the result as a string type. For example if we had the following native function:

```cpp
void CBCountry::GetCountryCodeForLocale(char szCountryCode[SIZEOF_SZCOUNTRYCODE])
{
	if (LocaleIsEire())
	{
		strcpy_s(szCountryCode, SIZEOF_SZCOUNTRYCODE, _T("IE"));
	}
	else	// default to GB
	{
		strcpy_s(szCountryCode, SIZEOF_SZCOUNTRYCODE, _T("GB"));
	}
}
```

We can create the following overload:

```cpp
CSGString CBCountry::GetCountryCodeForLocale()
{
	char szCountryCode[SIZEOF_SZCOUNTRYCODE];
	GetCountryCodeForLocale(szCountryCode);

	return szCountryCode;
}
```