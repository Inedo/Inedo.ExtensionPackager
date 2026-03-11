using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Inedo.ExtensionPackager;

internal sealed class PackageMetadata
{
    public string Group => "inedox";
    public required string Name { get; init; }
    public required string Version { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? Icon { get; init; }
    [JsonPropertyName("_inedoSdkVersion")]
    public required string SdkVersion { get; init; }
    [JsonPropertyName("_inedoProducts")]
    public string[]? Products { get; init; }
    [JsonPropertyName("_targetFrameworks")]
    public string[]? TargetFrameworks { get; init; }

    public void Write(ZipArchive zip)
    {
        var entry = zip.CreateEntry("upack.json", CompressionLevel.SmallestSize);
        using var stream = entry.Open();
        JsonSerializer.Serialize(stream, this, PackageMetadataJsonContext.Default.PackageMetadata);
    }
}
