using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

internal static class ConfigurationLoader
{
	private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
	{
		PropertyNameCaseInsensitive = true
	};

	public static async Task<ConfigurationBundle> LoadAsync(string baseDirectory)
	{
		string appVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";
		string text = LicenseDataCache.PathFor("offsets.json");
		string text2 = LicenseDataCache.PathFor("recipes.json");
		string text3 = (File.Exists(text) ? text : Path.Combine(baseDirectory, "offsets.json"));
		string recipesPath = (File.Exists(text2) ? text2 : Path.Combine(baseDirectory, "recipes.json"));
		if (!File.Exists(text3))
		{
			throw new FileNotFoundException("Missing offsets.json next to the application.", text3);
		}
		if (!File.Exists(recipesPath))
		{
			throw new FileNotFoundException("Missing recipes.json next to the application.", recipesPath);
		}
		OffsetsDocument doc = JsonSerializer.Deserialize<OffsetsDocument>(await File.ReadAllTextAsync(text3), JsonOptions) ?? throw new InvalidDataException("offsets.json is invalid.");
		if (doc.SchemaVersion != 1)
		{
			throw new InvalidDataException($"Unsupported offsets schema {doc.SchemaVersion}.");
		}
		if (!doc.Enabled)
		{
			throw new DataDisabledException(string.IsNullOrWhiteSpace(doc.Message) ? "Offset data has been disabled." : doc.Message);
		}
		EnsureCompatible(appVersion, doc.MinimumAppVersion, "offsets.json");
		if ((object)doc.Offsets == null)
		{
			throw new InvalidDataException("offsets.json does not contain an offsets object.");
		}
		Aob.Pattern[] patterns = doc.GameStatePatterns.Select(ParsePattern).ToArray();
		if (patterns.Length == 0)
		{
			throw new InvalidDataException("offsets.json contains no GameState AOB patterns.");
		}
		RuneMonolithCatalog catalog = await RuneMonolithCatalog.LoadFileAsync(recipesPath, appVersion);
		return new ConfigurationBundle(doc, doc.Offsets, patterns, catalog, appVersion);
	}

	private static Aob.Pattern ParsePattern(string text)
	{
		string[] array = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		if (array.Length < 7)
		{
			throw new InvalidDataException("Invalid AOB pattern: " + text);
		}
		return new Aob.Pattern(array.Select((string x) => ((!(x == "?") && !(x == "??")) || 1 == 0) ? new byte?(Convert.ToByte(x, 16)) : ((byte?)null)).ToArray(), 3, 7);
	}

	public static void EnsureCompatible(string current, string minimum, string source)
	{
		if (!Version.TryParse(current, out Version result) || !Version.TryParse(minimum, out Version result2))
		{
			throw new InvalidDataException("Invalid version metadata in " + source + ".");
		}
		if (result < result2)
		{
			throw new InvalidDataException($"{source} requires app {result2} or newer (installed: {result}).");
		}
	}
}
