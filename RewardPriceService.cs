using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

internal static class RewardPriceService
{
	private sealed class Root
	{
		public List<Item>? rewards { get; set; }
	}

	private sealed class Item
	{
		public string? name { get; set; }

		public double? price_exalted { get; set; }

		public double? price_divine { get; set; }
	}

	private const string Url = "https://raw.githubusercontent.com/tantran21501/P2Exchange/main/data/currency.json";

	private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
	{
		PropertyNameCaseInsensitive = true
	};

	public static async Task<(RewardPriceBook Book, string Source)> LoadAsync(CancellationToken token)
	{
		string cacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PoE2ExpeditionRadar");
		string cachePath = Path.Combine(cacheDir, "currency.json");
		string json = null;
		string source = "P2Exchange live";
		string path = LicenseDataCache.PathFor("prices.json");
		if (File.Exists(path))
		{
			json = await File.ReadAllTextAsync(path, token);
			source = "signed server data";
		}
		try
		{
			if (json == null)
			{
				using HttpClient http = new HttpClient
				{
					Timeout = TimeSpan.FromSeconds(12L)
				};
				json = await http.GetStringAsync("https://raw.githubusercontent.com/tantran21501/P2Exchange/main/data/currency.json", token);
				Directory.CreateDirectory(cacheDir);
				await File.WriteAllTextAsync(cachePath, json, token);
			}
		}
		catch when (!token.IsCancellationRequested && File.Exists(cachePath))
		{
			json = await File.ReadAllTextAsync(cachePath, token);
			source = "P2Exchange cache";
		}
		catch when (!token.IsCancellationRequested)
		{
			return (Book: RewardPriceBook.Empty, Source: "unavailable");
		}
		return (Book: new RewardPriceBook((JsonSerializer.Deserialize<Root>(json, JsonOptions)?.rewards ?? new List<Item>()).Select((Item x) => (x.name ?? "", price_exalted: x.price_exalted, price_divine: x.price_divine))), Source: source);
	}
}
