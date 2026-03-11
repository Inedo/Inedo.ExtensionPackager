using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Mono.Cecil;

namespace Inedo.ExtensionPackager;

internal partial record ExtensionInfo(string ContainingPath, string Name, Version Version, Version SdkVersion, string TargetFramework, InedoProduct Products, string? Title = null, string? Description = null, string? IconUrl = null)
{
    public static bool TryRead(string assemblyFileName, [NotNullWhen(true)] out ExtensionInfo? extensionAssemblyInfo)
    {
        extensionAssemblyInfo = null;

        using var asm = AssemblyDefinition.ReadAssembly(assemblyFileName);
        var module = asm.MainModule;
        if (module.HasAssemblyReferences)
        {
            foreach (var asmRef in module.AssemblyReferences)
            {
                if (asmRef.Name == "Inedo.SDK")
                {
                    extensionAssemblyInfo = ProcessExtension(Path.GetDirectoryName(assemblyFileName)!, asm, asmRef.Version);
                    return true;
                }
            }
        }

        return false;
    }

    private static ExtensionInfo ProcessExtension(string containingPath, AssemblyDefinition assembly, Version sdkVersion)
    {
        string? iconUrl = null;
        var supportedProducts = InedoProduct.Unspecified;
        string targetFramework;

        if (assembly.TryGetCustomAttribute("System.Runtime.Versioning.TargetFrameworkAttribute", out var targetFrameworkAttribute))
        {
            var f = targetFrameworkAttribute.ConstructorArguments[0].Value?.ToString();
            if (string.IsNullOrEmpty(f))
                throw new ConsoleException("TargetFrameworkAttribute was not found in extension assembly.");

            var m = TargetVersionRegex().Match(f);
            if (!m.Success)
                throw new ConsoleException($"Target framework {f} not supported.");

            targetFramework = $"net{m.Groups[1].ValueSpan}";
        }
        else
        {
            throw new ConsoleException("TargetFrameworkAttribute was not found in extension assembly.");
        }

        if (assembly.TryGetCustomAttribute("Inedo.Extensibility.ExtensionIconAttribute", out var iconAttribute))
            iconUrl = iconAttribute.ConstructorArguments[0].Value?.ToString();

        if (assembly.TryGetCustomAttribute("Inedo.Extensibility.AppliesToAttribute", out var appliesToAttribute))
        {
            // need to access by blob because the Inedo.SDK assembly can't be loaded from here
            var blob = appliesToAttribute.GetBlob();
            supportedProducts = (InedoProduct)BinaryPrimitives.ReadInt32LittleEndian(blob.AsSpan(2, 4));
        }

        string? title = null;
        if (assembly.TryGetCustomAttribute("System.Reflection.AssemblyTitleAttribute", out var titleAttribute))
            title = titleAttribute.ConstructorArguments[0].Value?.ToString();

        string? description = null;
        if (assembly.TryGetCustomAttribute("System.Reflection.AssemblyDescriptionAttribute", out var descriptionAttribute))
            description = descriptionAttribute.ConstructorArguments[0].Value?.ToString();

        return new ExtensionInfo(containingPath, assembly.Name.Name, assembly.Name.Version, sdkVersion, targetFramework, supportedProducts, title, description, iconUrl);
    }

    [GeneratedRegex(@"\A\.NETCoreApp,Version=v(?<1>[0-9]+\.[0-9]+)\z", RegexOptions.Singleline | RegexOptions.ExplicitCapture)]
    private static partial Regex TargetVersionRegex();
}
