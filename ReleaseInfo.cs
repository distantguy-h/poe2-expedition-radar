using System.Text.Json.Serialization;

internal sealed class ReleaseInfo
{
	[JsonPropertyName("version")]
	public string Version { get; set; } = "";

	[JsonPropertyName("offsets")]
	public ReleaseAsset Offsets { get; set; } = new ReleaseAsset();

	[JsonPropertyName("recipes")]
	public ReleaseAsset Recipes { get; set; } = new ReleaseAsset();

	[JsonPropertyName("prices")]
	public ReleaseAsset Prices { get; set; } = new ReleaseAsset();
}
