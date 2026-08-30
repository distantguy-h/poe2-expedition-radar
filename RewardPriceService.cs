using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

	private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
	{
		PropertyNameCaseInsensitive = true
	};

	public static async Task<(RewardPriceBook Book, string Source)> LoadAsync(CancellationToken token)
	{
		string json = null;
		string source = "bundled data";

		string serverPath = LicenseDataCache.PathFor("prices.json");
		if (File.Exists(serverPath))
		{
			json = await File.ReadAllTextAsync(serverPath, token);
			source = "cached server data";
		}

		if (json == null)
		{
			string localPath = Path.Combine(AppContext.BaseDirectory, "currency.json");
			if (File.Exists(localPath))
			{
				json = await File.ReadAllTextAsync(localPath, token);
				source = "bundled data";
			}
		}

		if (json == null)
		{
			string cachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PoE2ExpeditionRadar", "currency.json");
			if (File.Exists(cachePath))
			{
				json = await File.ReadAllTextAsync(cachePath, token);
				source = "local cache";
			}
		}

		if (json == null)
		{
			return (Book: RewardPriceBook.Empty, Source: "unavailable");
		}

		return (Book: new RewardPriceBook((JsonSerializer.Deserialize<Root>(json, JsonOptions)?.rewards ?? new List<Item>()).Select((Item x) => (x.name ?? "", price_exalted: x.price_exalted, price_divine: x.price_divine))), Source: source);
	}
}
