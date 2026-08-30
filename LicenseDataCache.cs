using System;
using System.IO;

internal static class LicenseDataCache
{
	public static string DirectoryPath { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PoE2ExpeditionRadar", "data");

	public static string PathFor(string name)
	{
		return Path.Combine(DirectoryPath, name);
	}
}
