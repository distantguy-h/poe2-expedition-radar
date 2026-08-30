using System.Text.Json.Serialization;

internal sealed class BootstrapEnvelope
{
	[JsonPropertyName("manifest")]
	public string Manifest { get; set; } = "";
}
