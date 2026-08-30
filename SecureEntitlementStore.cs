using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

internal sealed class SecureEntitlementStore
{
	private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("POE2 Expedition Radar licensing v1");

	private readonly string _directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PoE2ExpeditionRadar");

	private string LicensePath => Path.Combine(_directory, "license.bin");

	private string InstallPath => Path.Combine(_directory, "installation.bin");

	public StoredLicense? Load()
	{
		try
		{
			return JsonSerializer.Deserialize<StoredLicense>(Unprotect(File.ReadAllBytes(LicensePath)));
		}
		catch
		{
			return null;
		}
	}

	public void Save(StoredLicense value)
	{
		Directory.CreateDirectory(_directory);
		File.WriteAllBytes(LicensePath, Protect(JsonSerializer.SerializeToUtf8Bytes(value)));
	}

	public string? LoadInstallationId()
	{
		try
		{
			return Encoding.UTF8.GetString(Unprotect(File.ReadAllBytes(InstallPath)));
		}
		catch
		{
			return null;
		}
	}

	public void SaveInstallationId(string value)
	{
		Directory.CreateDirectory(_directory);
		File.WriteAllBytes(InstallPath, Protect(Encoding.UTF8.GetBytes(value)));
	}

	private static byte[] Protect(byte[] value)
	{
		return ProtectedData.Protect(value, Entropy, DataProtectionScope.CurrentUser);
	}

	private static byte[] Unprotect(byte[] value)
	{
		return ProtectedData.Unprotect(value, Entropy, DataProtectionScope.CurrentUser);
	}
}
