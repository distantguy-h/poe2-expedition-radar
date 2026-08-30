using System.Text.Json.Serialization;

internal sealed record ActivationResponse([property: JsonPropertyName("entitlement")] string Entitlement, [property: JsonPropertyName("refresh_token")] string RefreshToken, [property: JsonPropertyName("license")] LicenseResponse License);
