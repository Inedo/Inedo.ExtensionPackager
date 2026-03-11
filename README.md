# Inedo.ExtensionPackager
This tool is intended to pack and validate extensions for Inedo products. Extensions are .NET class library projects that target one or more compatible frameworks,
and are packaged into a ProGet universal package.

## Installation
Install from NuGet using `dotnet tool`:
````console
dotnet tool install [--global] Inedo.ExtensionPackager
````
In a development environment, you may want to use the `--global` flag so the tool will be available from any path. See [Install and use a .NET global tool using the .NET CLI](https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools-how-to-use)
in Microsoft's documentation for more information about using dotnet tools.

## Usage
The Inedo.ExtensionPackager tool registers the `inedoxpack` command, and can be invoked with:
````console
dotnet inedoxpack
````
To package an Inedo extension, create a class library that targets one or more supported frameworks, and reference the `Inedo.SDK` NuGet package. See an existing repository such as [InedoCore](https://github.com/Inedo/inedox-inedocore)
or [DotNet](https://github.com/Inedo/inedox-dotnet) for examples of how to create an extension.

The easiest way to package an extension is to build it with `inedoxpack`:

````console
dotnet inedoxpack pack <MyExtensionProjectPath> <MyExtension.upack> --build=<Debug/Release>
````
This will invoke `dotnet publish` with all of the required arguments and package the output into the target package.

For example, if you have `/home/me/MyExtension/MyExtension.csproj` as your project file, you would run:
````console
dotnet inedoxpack pack /home/me/MyExtension MyExtension.upack --build=Release
````
This would create a release build of your extension and save it to `MyExtension.upack` in the working directory.

It's also possible to build the extension yourself using `dotnet publish` and then invoke `inedoxpack` on its output:

````console
dotnet publish -f net10.0 -c Release -o bin\pub
dotnet inedoxpack pack bin\pub
````

## Configuration
Set the `INEDOXPACK_OUTDIR` environment variable to define a default output directory for packages. If this variable is not set, packages will be created in the current directory unless
an absolute path is specified.
