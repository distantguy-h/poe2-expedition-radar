using System.Text.Json.Serialization;

internal sealed class ReleaseManifest
{
	[JsonPropertyName("type")]
	public string Type { get; set; } = "";

	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; }

	[JsonPropertyName("message")]
	public string Message { get; set; } = "";

	[JsonPropertyName("release")]
	public ReleaseInfo Release { get; set; } = new ReleaseInfo();
}
