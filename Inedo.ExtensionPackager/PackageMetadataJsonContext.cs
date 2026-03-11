using System.Text.Json.Serialization;

namespace Inedo.ExtensionPackager;

[JsonSerializable(typeof(PackageMetadata))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, WriteIndented = true)]
internal sealed partial class PackageMetadataJsonContext : JsonSerializerContext
{
}
