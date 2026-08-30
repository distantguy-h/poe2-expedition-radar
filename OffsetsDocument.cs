using System;
using System.Collections.Generic;

internal sealed class OffsetsDocument
{
	public int SchemaVersion { get; set; }

	public string DataVersion { get; set; } = "";

	public string AppVersion { get; set; } = "";

	public string GameBuild { get; set; } = "";

	public DateTime UpdatedUtc { get; set; }

	public bool Enabled { get; set; } = true;

	public string Message { get; set; } = "";

	public string MinimumAppVersion { get; set; } = "1.0.0";

	public List<string> GameStatePatterns { get; set; } = new List<string>();

	public RuntimeOffsets? Offsets { get; set; }
}
