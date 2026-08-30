using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

internal sealed class DeviceIdentityService
{
	private readonly SecureEntitlementStore _store;

	public string InstallationId { get; }

	public string InstallationHash => Sha256(InstallationId);

	public string DeviceHash { get; }

	public DeviceIdentityService(SecureEntitlementStore store)
	{
		_store = store;
		InstallationId = store.LoadInstallationId() ?? Guid.NewGuid().ToString("N");
		store.SaveInstallationId(InstallationId);
		string text = Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Cryptography", "MachineGuid", "")?.ToString() ?? "unknown";
		DeviceHash = Sha256("poe2-expedition-radar|" + text.Trim().ToLowerInvariant());
	}

	private static string Sha256(string value)
	{
		return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
	}
}
