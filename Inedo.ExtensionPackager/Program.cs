using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Inedo.UPack;
using Inedo.UPack.Packaging;

namespace Inedo.ExtensionPackager
{
    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            try
            {
                var inputArgs = InputArgs.Parse(args);
                if (inputArgs.Positional.Length == 0)
                {
                    await WriteUsageAsync();
                    return 1;
                }

                await (inputArgs.Positional[0] switch
                {
                    "pack" => PackAsync(inputArgs),
                    "help" => WriteUsageAsync(),
                    _ => throw new ConsoleException($"Invalid command: {inputArgs.Positional[0]}")
                });

                return 0;
            }
            catch (ConsoleException ex)
            {
                Console.Error.WriteLine(ex.Message);
                return -1;
            }
        }

        private static Task WriteUsageAsync()
        {
            Console.WriteLine("Usage: inedoxpack pack [SourceDirectory] [Output.upack] [-o] [--name=<name override>] [--version=<version override] [--icon-url=<icon url override>]");
            Console.WriteLine("If SourceDirectory is not specified, the current directory is used.");
            Console.WriteLine("If an output file is not specified, <PackageName>.upack will be used.");
            Console.WriteLine("Set the INEDOXPACK_OUTDIR environment variable to output the package to a different default directory instead of the current directory.");
            return Task.CompletedTask;
        }
        private static async Task PackAsync(InputArgs inputArgs)
        {
            var sourceDirectory = Environment.CurrentDirectory;
            string? outputFileName = null;

            if (inputArgs.Positional.Length > 1)
                sourceDirectory = Path.Combine(sourceDirectory, inputArgs.Positional[1]);
            if (inputArgs.Positional.Length > 2)
                outputFileName = inputArgs.Positional[2];
            if (inputArgs.Positional.Length > 3)
                throw new ConsoleException($"Unexpected argument: {inputArgs.Positional[3]}");

            UniversalPackageVersion? overriddenVersion = null;
            if (inputArgs.Named.TryGetValue("version", out var versionText))
                overriddenVersion = UniversalPackageVersion.TryParse(versionText) ?? throw new ConsoleException("Invalid version specified for --version argument.");

            var infos = GetExtensionInfos(sourceDirectory, inputArgs.Named.GetValueOrDefault("name"));
            if (infos.Count == 0)
            {
                // this should never happen
                throw new ConsoleException($"No extensions were found in {sourceDirectory}");
            }

            foreach (var info in infos)
                Console.WriteLine($"{info.ContainingPath}: found {info.Name} ({GetTargetFrameworkName(info.TargetFramework)})");

            string? defaultIconUrl = null;

            var first = infos[0];
            var frameworks = first.TargetFramework;

            if (infos.Count > 1)
            {
                for (int i = 1; i < infos.Count; i++)
                {
                    if (frameworks.HasFlag(infos[i].TargetFramework))
                        throw new ConsoleException($"Found multiple extensions targeting {GetTargetFrameworkName(infos[i].TargetFramework)}.");

                    frameworks |= infos[i].TargetFramework;
                    assertSame(first.Name, infos[i].Name, "assembly name");
                    assertSame(first.Title, infos[i].Title, "AsssemblyTitleAttribute");
                    assertSame(first.Description, infos[i].Description, "AsssemblyDescriptionAttribute");
                    assertSame(first.SdkVersion, infos[i].SdkVersion, "referenced Inedo SDK version");
                    assertSame(first.Products, infos[i].Products, "AppliesToAttribute");
                    if (overriddenVersion == null)
                        assertSame(first.Version, infos[i].Version, "assembly version");
                }

                static void assertSame<T>(T expected, T actual, string propertyName)
                {
                    if (!EqualityComparer<T>.Default.Equals(expected, actual))
                        throw new ConsoleException($"Inconsistent {propertyName} in multitargeted extension.");
                }
            }

            Console.WriteLine($"Name: {first.Name}");
            Console.WriteLine($"Title: {first.Title}");
            Console.WriteLine($"SDK version: {first.SdkVersion.ToString(3)}");

            var metadata = GetPackageMetadata(first, defaultIconUrl, overriddenVersion, frameworks);

            if (string.IsNullOrEmpty(outputFileName))
                outputFileName = $"{metadata.Name}.upack";

            var outDir = Environment.GetEnvironmentVariable("INEDOXPACK_OUTDIR") ?? Environment.CurrentDirectory;
            outputFileName = Path.Combine(outDir, outputFileName);

            if (File.Exists(outputFileName))
            {
                if (inputArgs.Named.ContainsKey("o"))
                    File.Delete(outputFileName);
                else
                    throw new ConsoleException($"{outputFileName} already exists. Specify -o to overwrite.");
            }

            Console.WriteLine($"Writing {outputFileName}...");
            using (var upack = new UniversalPackageBuilder(outputFileName, metadata))
            {
                if (infos.Count == 1)
                {
                    await upack.AddContentsAsync(first.ContainingPath, string.Empty, true);
                }
                else
                {
                    foreach (var info in infos)
                        await upack.AddContentsAsync(info.ContainingPath, GetTargetFrameworkName(info.TargetFramework), true);
                }
            }

            Console.WriteLine("Package created.");
        }
        private static IReadOnlyList<ExtensionInfo> GetExtensionInfos(string path, string? name)
        {
            if (path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"Reading {path}...");

                if (name != null && Path.GetFileNameWithoutExtension(path) != name)
                    throw new ConsoleException($"Extension assembly {path} has incorrect name (expected {name}).");

                // known name, single dll
                if (!ExtensionInfo.TryRead(path, out var info))
                    throw new ConsoleException($"Invalid extension assembly: {path}");

                return new[] { info };
            }
            else if (!Directory.Exists(path))
            {
                throw new ConsoleException($"{path} not found.");
            }

            Console.WriteLine($"Searching {path} for extensions...");

            var infos = new List<ExtensionInfo>(2);

            foreach (var subdir in Directory.EnumerateDirectories(path))
            {
                if (Path.GetFileName(subdir) == "net452")
                {
                    Console.WriteLine("Found net452 subdirectory; looking for net452 extension...");

                    var info = scanDirectory(subdir);
                    if (info.TargetFramework != ExtensionTargetFramework.Net452)
                        throw new ConsoleException($"Expected net452 extenion in {subdir} but found {GetTargetFrameworkName(info.TargetFramework)} instead.");

                    infos.Add(info);
                }
                else if (Path.GetFileName(subdir) == "net5.0")
                {
                    Console.WriteLine("Found net5.0 subdirectory; looking for net5.0 extension...");

                    var info = scanDirectory(subdir);
                    if (info.TargetFramework != ExtensionTargetFramework.Net50)
                        throw new ConsoleException($"Expected net5.0 extenion in {subdir} but found {GetTargetFrameworkName(info.TargetFramework)} instead.");

                    infos.Add(info);
                }
            }

            if (infos.Count > 0)
                return infos;

            return new[] { scanDirectory(path) };

            ExtensionInfo scanDirectory(string dir)
            {
                if (name != null)
                {
                    var fullPath = Path.Combine(dir, $"{name}.dll");
                    if (!File.Exists(fullPath))
                        throw new ConsoleException($"{fullPath} not found.");

                    if (!ExtensionInfo.TryRead(fullPath, out var info))
                        throw new ConsoleException($"Invalid extension: {fullPath}");

                    return info;
                }

                ExtensionInfo? found = null;

                foreach (var fileName in Directory.EnumerateFiles(dir, "*.dll"))
                {
                    if (ExtensionInfo.TryRead(fileName, out var info))
                    {
                        if (found != null)
                            throw new ConsoleException("Found more than one assembly that references Inedo.SDK. Use the --extension-name argument to specify the primary assembly name.");

                        found = info;
                    }
                }

                if (found == null)
                    throw new ConsoleException($"No extensions were found in {dir}");

                return found;
            }

        }
        private static UniversalPackageMetadata GetPackageMetadata(ExtensionInfo info, string? defaultIconUrl, UniversalPackageVersion? versionOverride, ExtensionTargetFramework frameworks)
        {
            var products = new List<string>(3);
            if (info.Products.HasFlag(InedoProduct.BuildMaster))
                products.Add("buildmaster");
            if (info.Products.HasFlag(InedoProduct.Otter))
                products.Add("otter");
            if (info.Products.HasFlag(InedoProduct.ProGet))
                products.Add("proget");

            var targetFrameworks = new List<string>(2);
            if (frameworks.HasFlag(ExtensionTargetFramework.Net452))
                targetFrameworks.Add("net452");
            if (frameworks.HasFlag(ExtensionTargetFramework.Net50))
                targetFrameworks.Add("net5.0");

            return new UniversalPackageMetadata
            {
                Name = info.Name,
                Version = versionOverride ?? new(info.Version.Major, info.Version.Minor, Math.Max(info.Version.Build, 0)),
                Title = info.Title,
                Description = info.Description,
                Icon = info.IconUrl ?? defaultIconUrl,
                ["_inedoSdkVersion"] = info.SdkVersion.ToString(3),
                ["_inedoProducts"] = products,
                ["_targetFrameworks"] = targetFrameworks
            };
        }
        private static string GetTargetFrameworkName(ExtensionTargetFramework f)
        {
            return f switch
            {
                ExtensionTargetFramework.Net452 => "net452",
                ExtensionTargetFramework.Net50 => "net5.0",
                _ => throw new ArgumentOutOfRangeException(nameof(f))
            };
        }
    }
}
