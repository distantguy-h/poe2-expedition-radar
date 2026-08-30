using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

internal sealed class LicenseApiClient
{
	private readonly HttpClient _http = new HttpClient
	{
		Timeout = TimeSpan.FromSeconds(12L)
	};

	public async Task<ActivationResponse> ActivateAsync(string key, DeviceIdentityService device, string appVersion, CancellationToken token)
	{
		return await SendAsync("v1/licenses/activate", new
		{
			license_key = key,
			device_hash = device.DeviceHash,
			installation_hash = device.InstallationHash,
			app_version = appVersion
		}, token);
	}

	public async Task<ActivationResponse> RefreshAsync(string refreshToken, DeviceIdentityService device, string appVersion, CancellationToken token)
	{
		return await SendAsync("v1/auth/refresh", new
		{
			refresh_token = refreshToken,
			device_hash = device.DeviceHash,
			app_version = appVersion
		}, token);
	}

	private async Task<ActivationResponse> SendAsync(string path, object body, CancellationToken token)
	{
		using HttpResponseMessage response = await _http.PostAsJsonAsync(path, body, token);
		if (response.IsSuccessStatusCode)
		{
			return (await response.Content.ReadFromJsonAsync<ActivationResponse>(token)) ?? throw new InvalidDataException("License server returned an empty response.");
		}
		string json = await response.Content.ReadAsStringAsync(token);
		string text = "SERVER_ERROR";
		try
		{
			text = JsonDocument.Parse(json).RootElement.GetProperty("code").GetString() ?? text;
		}
		catch
		{
		}
		throw new LicenseApiException(text, response.StatusCode);
	}
}
