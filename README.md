# Inedo.ExtensionPackager
This tool is intended to pack and validating extensions for Inedo products. Extensions are .NET class library projects that target one or more compatible frameworks (currently net452 and net5.0),
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
dotnet tool inedoxpack
````
To package an Inedo extension, create a class library that targets one or more supported frameworks, and reference the `Inedo.SDK` NuGet package. See an existing repository such as [InedoCore](https://github.com/Inedo/inedox-inedocore)
or [DotNet](https://github.com/Inedo/inedox-dotnet) for examples of how to create an extension. Once you are ready to build, it's recommended to do a `dotnet publish` command to make sure all
dependencies are available:
````console
dotnet publish -f net452 -c Release -o bin\pub\net452
dotnet publish -f net5.0 -c Release -o bin\pub\net5.0
dotnet tool inedoxpack bin\pub
````
This will create `<ExtensionName>.upack` in the current directory.