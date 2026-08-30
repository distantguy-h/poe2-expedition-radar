using System;
using System.Collections.Generic;
using System.Linq;

internal static class Bootstrap
{
	public static nint ResolveGameStateSlot(GameProcess process, MemoryReader mem)
	{
		return ResolveGameStateSlot(process, mem, new RuntimeOffsets(1472, 1760, 196, 8, 656, 8, 8, 40, 16, 32, 16, 40, 48, 56, 60, 160), Aob.GameStateRefs);
	}

	public static nint ResolveGameStateSlot(GameProcess process, MemoryReader mem, RuntimeOffsets offsets, IEnumerable<Aob.Pattern> patterns)
	{
		int num = 0;
		foreach (Aob.Pattern pattern in patterns)
		{
			foreach (nint item in AobScanner.ScanForResolvedAddresses(process, mem, pattern).Distinct())
			{
				num++;
				if (TryResolveInGame(mem, item, offsets, out var inGameState, out var area, out var localPlayer))
				{
					Console.WriteLine($"  Resolved InGameState: 0x{inGameState:X}");
					Console.WriteLine($"  Resolved AreaInstance: 0x{area:X}");
					Console.WriteLine($"  Resolved LocalPlayer: 0x{localPlayer:X}");
					return item;
				}
			}
		}
		Console.WriteLine((num == 0) ? "[bootstrap] No GameState AOB matches were found." : $"[bootstrap] Found {num} AOB candidate(s), but none produced a valid in-game chain.");
		return 0;
	}

	public static bool TryResolveInGame(MemoryReader mem, nint gameStateSlot, out nint inGameState, out nint area, out nint localPlayer)
	{
		return TryResolveInGame(mem, gameStateSlot, null, out inGameState, out area, out localPlayer);
	}

	public static bool TryResolveInGame(MemoryReader mem, nint gameStateSlot, RuntimeOffsets? offsets, out nint inGameState, out nint area, out nint localPlayer)
	{
		inGameState = (area = (localPlayer = 0));
		nint num = mem.Ptr(gameStateSlot);
		if (num == 0)
		{
			return false;
		}
		List<nint> list = new List<nint>(13);
		int num2 = offsets?.StateCurrent ?? 8;
		int num3 = offsets?.InGameArea ?? 656;
		nint num4 = mem.Ptr(num + num2);
		if (num4 != 0)
		{
			list.Add(mem.Ptr(num4));
		}
		for (int i = 0; i < 12; i++)
		{
			list.Add(mem.Ptr(num + 72 + i * 16));
		}
		foreach (nint item in list.Distinct())
		{
			if (item == 0)
			{
				continue;
			}
			nint num5 = mem.Ptr(item + num3);
			if (num5 == 0)
			{
				continue;
			}
			foreach (int item2 in CandidateLocalPlayerOffsets(offsets?.AreaLocalPlayer ?? 1472))
			{
				nint num6 = mem.Ptr(num5 + item2);
				if (num6 != 0 && EntityReader.ReadMetadata(mem, num6).StartsWith("Metadata/Characters/", StringComparison.Ordinal))
				{
					inGameState = item;
					area = num5;
					localPlayer = num6;
					return true;
				}
			}
		}
		return false;
	}

	private static IEnumerable<int> CandidateLocalPlayerOffsets(int configured)
	{
		yield return configured;
		for (int off = 1024; off <= 1920; off += 8)
		{
			if (off != configured)
			{
				yield return off;
			}
		}
	}
}
