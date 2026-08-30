using System;
using System.Text.Json.Serialization;

internal sealed class EntitlementClaims
{
	[JsonPropertyName("license_id")]
	public string LicenseId { get; set; } = "";

	[JsonPropertyName("activation_id")]
	public string ActivationId { get; set; } = "";

	[JsonPropertyName("device_hash")]
	public string DeviceHash { get; set; } = "";

	[JsonPropertyName("plan")]
	public string Plan { get; set; } = "";

	[JsonPropertyName("features")]
	public string[] Features { get; set; } = Array.Empty<string>();

	[JsonPropertyName("online_check_after")]
	public DateTimeOffset OnlineCheckAfter { get; set; }

	[JsonPropertyName("license_expires_at")]
	public DateTimeOffset LicenseExpiresAt { get; set; }

	[JsonPropertyName("expires_at")]
	public DateTimeOffset ExpiresAt { get; set; }
}
