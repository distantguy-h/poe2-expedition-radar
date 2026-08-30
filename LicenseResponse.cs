using System;
using System.Text.Json.Serialization;

internal sealed record LicenseResponse([property: JsonPropertyName("plan")] string Plan, [property: JsonPropertyName("status")] string Status, [property: JsonPropertyName("expires_at")] DateTimeOffset ExpiresAt);
