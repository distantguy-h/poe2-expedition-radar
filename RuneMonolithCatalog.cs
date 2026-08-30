using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

internal sealed class RuneMonolithCatalog
{
	private sealed class Root
	{
		public int schemaVersion { get; set; }

		public string dataVersion { get; set; } = "";

		public DateTime updatedUtc { get; set; }

		public bool enabled { get; set; } = true;

		public string message { get; set; } = "";

		public string minimumAppVersion { get; set; } = "1.0.0";

		public Dictionary<string, string>? runes { get; set; }

		public List<Recipe>? recipes { get; set; }

		public List<Weight>? runeWeights { get; set; }
	}

	private sealed class Weight
	{
		public int rune { get; set; }

		public int pos { get; set; }

		public int size { get; set; }

		public int minLevel { get; set; }
	}

	private sealed class Reward
	{
		public string? name { get; set; }
	}

	private sealed class Recipe
	{
		public string? id { get; set; }

		public int size { get; set; }

		public List<int>? runeIdx { get; set; }

		public List<string>? runes { get; set; }

		public Reward? reward { get; set; }

		public int rewardCount { get; set; }

		public string? description { get; set; }

		public int minLevel { get; set; }

		public int maxLevel { get; set; }
	}

	public readonly record struct Offer(string Name, int Count, int Size, string Runes);

	private readonly List<Recipe> _recipes;

	private readonly Dictionary<long, int> _partial;

	public IReadOnlyDictionary<int, string> RuneNames { get; }

	public string DataVersion { get; }

	public int RecipeCount => _recipes.Count;

	private RuneMonolithCatalog(Root root)
	{
		_recipes = root.recipes ?? new List<Recipe>();
		DataVersion = root.dataVersion;
		_partial = new Dictionary<long, int>();
		foreach (Weight item in root.runeWeights ?? new List<Weight>())
		{
			long key = Key(item.rune, item.pos, item.size);
			if (!_partial.TryGetValue(key, out var value) || item.minLevel < value)
			{
				_partial[key] = item.minLevel;
			}
		}
		Dictionary<int, string> dictionary = new Dictionary<int, string>();
		if (root.runes != null)
		{
			foreach (KeyValuePair<string, string> rune in root.runes)
			{
				if (int.TryParse(rune.Key, out var result))
				{
					dictionary[result] = rune.Value;
				}
			}
		}
		foreach (Recipe recipe in _recipes)
		{
			if (recipe.runeIdx != null && recipe.runes != null)
			{
				for (int i = 0; i < Math.Min(recipe.runeIdx.Count, recipe.runes.Count); i++)
				{
					dictionary.TryAdd(recipe.runeIdx[i], recipe.runes[i]);
				}
			}
		}
		RuneNames = dictionary;
	}

	private static long Key(int rune, int pos, int size)
	{
		return ((long)rune << 16) | ((long)pos << 8) | (uint)size;
	}

	public static async Task<RuneMonolithCatalog> LoadFileAsync(string path, string appVersion)
	{
		Root root = JsonSerializer.Deserialize<Root>(await File.ReadAllTextAsync(path), new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true
		}) ?? throw new InvalidDataException("Invalid recipe JSON.");
		if (root.schemaVersion != 1)
		{
			throw new InvalidDataException($"Unsupported recipes schema {root.schemaVersion}.");
		}
		if (!root.enabled)
		{
			throw new DataDisabledException(string.IsNullOrWhiteSpace(root.message) ? "Recipe data has been disabled." : root.message);
		}
		ConfigurationLoader.EnsureCompatible(appVersion, root.minimumAppVersion, "recipes.json");
		return new RuneMonolithCatalog(root);
	}

	public List<Offer> Offers(ExpeditionState s, int areaLevel)
	{
		List<Offer> list = new List<Offer>();
		if (!s.Resolved || s.HoleCount <= 0)
		{
			return list;
		}
		foreach (Recipe recipe in _recipes)
		{
			if (recipe.size <= 0 || recipe.size > s.HoleCount || (areaLevel > 0 && recipe.maxLevel > 0 && (areaLevel < recipe.minLevel || areaLevel > recipe.maxLevel)))
			{
				continue;
			}
			if (s.IsUnique || s.AnchorIdx < 0)
			{
				list.Add(ToOffer(recipe));
			}
			else if (recipe.runeIdx != null && s.AnchorPos >= 0 && recipe.runeIdx.Count > s.AnchorPos && recipe.runeIdx[s.AnchorPos] == s.AnchorIdx)
			{
				int value = 0;
				if ((recipe.size == s.HoleCount || _partial.TryGetValue(Key(s.AnchorIdx, s.AnchorPos + 1, recipe.size), out value)) && (recipe.size == s.HoleCount || areaLevel <= 0 || areaLevel >= value))
				{
					list.Add(ToOffer(recipe));
				}
			}
		}
		return list;
	}

	private static Offer ToOffer(Recipe r)
	{
		return new Offer(r.reward?.name ?? "Unknown", Math.Max(1, r.rewardCount), r.size, (r.runes == null) ? "" : string.Join(" · ", r.runes));
	}
}
