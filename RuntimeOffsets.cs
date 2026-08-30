using System.IO;
using System.Text.Json;

internal sealed record RuntimeOffsets(int AreaLocalPlayer, int AreaAwakeEntities, int AreaLevel, int StateCurrent, int InGameArea, int EntityDetails, int DetailsName, int DetailsLookup, int EntityComponents, int StateMachineListener, int RuneOwner, int RuneAnchorRef, int RuneAnchorHolder, int RuneHoleCount, int RuneAnchorPos, int RuneListenerSub, int RuneStride = 104, int RuneCount = 34, int AreaSleepingEntities = 0)
{
	public void Save(string path)
	{
		File.WriteAllText(path, JsonSerializer.Serialize(this, new JsonSerializerOptions
		{
			WriteIndented = true
		}));
	}
}
