using System.Text.Json.Serialization;

internal sealed class ReleaseAsset
{
	[JsonPropertyName("url")]
	public string Url { get; set; } = "";

	[JsonPropertyName("sha256")]
	public string Sha256 { get; set; } = "";
}
