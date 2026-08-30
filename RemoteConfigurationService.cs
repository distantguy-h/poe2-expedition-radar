using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

internal sealed class RemoteConfigurationService
{
	private readonly HttpClient _http = new HttpClient
	{
		BaseAddress = LicenseConfiguration.ApiBaseUri,
		Timeout = TimeSpan.FromSeconds(20L)
	};

	public async Task<string> RefreshAsync(CancellationToken token = default(CancellationToken))
	{
		try
		{
			if (!SignedPayloadVerifier.TryVerify<ReleaseManifest>(((await _http.GetFromJsonAsync<BootstrapEnvelope>("v1/client/bootstrap", token)) ?? throw new InvalidDataException("Bootstrap response was empty.")).Manifest, out ReleaseManifest manifest) || manifest?.Type != "release_manifest")
			{
				throw new CryptographicException("Release manifest signature is invalid.");
			}
			if (!manifest.Enabled)
			{
				throw new InvalidDataException(string.IsNullOrWhiteSpace(manifest.Message) ? "Scanner data is disabled." : manifest.Message);
			}
			Directory.CreateDirectory(LicenseDataCache.DirectoryPath);
			await DownloadAsync(manifest.Release.Offsets, "offsets.json", token);
			await DownloadAsync(manifest.Release.Recipes, "recipes.json", token);
			await DownloadAsync(manifest.Release.Prices, "prices.json", token);
			return manifest.Release.Version;
		}
		catch
		{
			return "offline (local data)";
		}
	}

	private async Task DownloadAsync(ReleaseAsset asset, string name, CancellationToken token)
	{
		byte[] array = await _http.GetByteArrayAsync(asset.Url, token);
		if (!string.Equals(Convert.ToHexStringLower(SHA256.HashData(array)), asset.Sha256, StringComparison.OrdinalIgnoreCase))
		{
			throw new CryptographicException(name + " failed SHA-256 verification.");
		}
		string destination = LicenseDataCache.PathFor(name);
		string temporary = destination + ".tmp";
		await File.WriteAllBytesAsync(temporary, array, token);
		File.Move(temporary, destination, overwrite: true);
	}
}
