using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using Mono.Cecil;

namespace Inedo.ExtensionPackager
{
    internal record ExtensionInfo(string ContainingPath, string Name, Version Version, Version SdkVersion, ExtensionTargetFramework TargetFramework, InedoProduct Products, string? Title = null, string? Description = null, string? IconUrl = null)
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
            ExtensionTargetFramework targetFramework;

            const string net452 = ".NETFramework,Version=v4.5.2";
            const string net50 = ".NETCoreApp,Version=v5.0";
            const string net60 = ".NETCoreApp,Version=v6.0";
            const string net80 = ".NETCoreApp,Version=v8.0";

            if (assembly.TryGetCustomAttribute("System.Runtime.Versioning.TargetFrameworkAttribute", out var targetFrameworkAttribute))
            {
                targetFramework = targetFrameworkAttribute.ConstructorArguments[0].Value?.ToString() switch
                {
                    net452 => ExtensionTargetFramework.Net452,
                    net50 => ExtensionTargetFramework.Net50,
                    net60 => ExtensionTargetFramework.Net60,
                    net80 => ExtensionTargetFramework.Net80,
                    _ => throw new Exception()
                };
            }
            else
            {
                throw new Exception();
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
    }
}
