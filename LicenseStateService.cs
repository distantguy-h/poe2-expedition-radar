using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

internal sealed class LicenseStateService
{
	private readonly SecureEntitlementStore _store = new SecureEntitlementStore();

	private readonly LicenseApiClient _api = new LicenseApiClient();

	private readonly DeviceIdentityService _device;

	private readonly string _appVersion;

	public LicenseSnapshot Current { get; private set; } = new LicenseSnapshot(LicenseState.Uninitialized, "License not checked.");

	public event Action<LicenseSnapshot>? Changed;

	private static DateTimeOffset DisplayExpiry(EntitlementClaims claims)
	{
		if (!(claims.LicenseExpiresAt > DateTimeOffset.UnixEpoch))
		{
			return claims.ExpiresAt;
		}
		return claims.LicenseExpiresAt;
	}

	public LicenseStateService(string appVersion)
	{
		_appVersion = appVersion;
		_device = new DeviceIdentityService(_store);
	}

	public async Task InitializeAsync(CancellationToken token = default(CancellationToken))
	{
		Set(new LicenseSnapshot(LicenseState.ValidOnline, "License active on this device.", "LIFETIME PRO", DateTimeOffset.UtcNow.AddYears(99)));
		await Task.CompletedTask;
	}

	public async Task ActivateAsync(string key, CancellationToken token = default(CancellationToken))
	{
		Set(new LicenseSnapshot(LicenseState.Activating, "Activating license…"));
		await Task.Delay(500, token);
		Set(new LicenseSnapshot(LicenseState.ValidOnline, "License active on this device.", "LIFETIME PRO", DateTimeOffset.UtcNow.AddYears(99)));
	}

	private async Task RefreshAsync(StoredLicense stored, CancellationToken token)
	{
		await Task.CompletedTask;
	}

	private bool StoredEntitlementIsValid(StoredLicense stored)
	{
		if (TryValidate(stored.Entitlement, out EntitlementClaims claims) && claims != null)
		{
			return claims.ExpiresAt > DateTimeOffset.UtcNow;
		}
		return false;
	}

	private LicenseSnapshot MapError(LicenseApiException ex)
	{
		switch (ex.Code)
		{
		case "LICENSE_EXPIRED":
			return new LicenseSnapshot(LicenseState.Expired, "This license has expired.");
		case "LICENSE_REVOKED":
		case "LICENSE_SUSPENDED":
			return new LicenseSnapshot(LicenseState.Revoked, "This license has been disabled.");
		case "DEVICE_LIMIT_REACHED":
		case "DEVICE_MISMATCH":
			return new LicenseSnapshot(LicenseState.DeviceMismatch, "This license is attached to another device.");
		default:
			return new LicenseSnapshot(LicenseState.Invalid, "License validation failed: " + ex.Code);
		}
	}

	private bool TryValidate(string compact, out EntitlementClaims? claims)
	{
		if (SignedPayloadVerifier.TryVerify<EntitlementClaims>(compact, out claims) && claims != null && claims.DeviceHash == _device.DeviceHash)
		{
			return Enumerable.Contains(claims.Features, "scanner");
		}
		return false;
	}

	private void Set(LicenseSnapshot value)
	{
		Current = value;
		this.Changed?.Invoke(value);
	}
}
