using System;
using System.Collections.Generic;

internal static class OffsetResolver
{
	public static RuntimeOffsets Discover(MemoryReader mem, nint gameStateSlot)
	{
		Console.WriteLine("[offset] discovering runtime layout...");
		if (!Bootstrap.TryResolveInGame(mem, gameStateSlot, out var _, out var area, out var _))
		{
			throw new InvalidOperationException("The live GameState chain is no longer valid.");
		}
		int num = FindLocalPlayerOffset(mem, area);
		if (num < 0)
		{
			throw new InvalidOperationException("Could not discover AreaInstance.LocalPlayer offset.");
		}
		int num2 = FindAwakeEntitiesOffset(mem, area);
		if (num2 < 0)
		{
			throw new InvalidOperationException("Could not discover AreaInstance.AwakeEntities offset.");
		}
		int num3 = FindAreaLevelOffset(mem, area);
		(int, int, int, int, int, int) tuple = DiscoverRuneLayout(mem, area, num2);
		RuntimeOffsets result = new RuntimeOffsets(num, num2, num3, 8, 656, 8, 8, 40, 16, 32, tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5, tuple.Item6);
		Console.WriteLine($"[offset] LocalPlayer    +0x{num:X}");
		Console.WriteLine($"[offset] AwakeEntities   +0x{num2:X}");
		Console.WriteLine($"[offset] AreaLevel       +0x{num3:X}");
		Console.WriteLine($"[offset] RuneStation     owner=+0x{tuple.Item1:X} anchor=+0x{tuple.Item2:X} holes=+0x{tuple.Item4:X} pos=+0x{tuple.Item5:X} listener=+0x{tuple.Item6:X}");
		return result;
	}

	private static int FindLocalPlayerOffset(MemoryReader mem, nint area)
	{
		for (int i = 1024; i <= 1792; i += 8)
		{
			nint num = mem.Ptr(area + i);
			if (num != 0 && EntityReader.ReadMetadata(mem, num).StartsWith("Metadata/Characters/", StringComparison.Ordinal))
			{
				return i;
			}
		}
		return -1;
	}

	private static int FindAwakeEntitiesOffset(MemoryReader mem, nint area)
	{
		for (int i = 1536; i <= 1920; i += 8)
		{
			nint num = mem.Ptr(area + i);
			if (num != 0)
			{
				nint num2 = mem.Ptr(num + 8);
				if (num2 != 0 && TryMapHasEntity(mem, num, num2))
				{
					return i;
				}
			}
		}
		return -1;
	}

	private static bool TryMapHasEntity(MemoryReader mem, nint head, nint root)
	{
		Queue<nint> queue = new Queue<nint>();
		queue.Enqueue(root);
		HashSet<nint> hashSet = new HashSet<nint>();
		while (queue.Count > 0 && hashSet.Count < 80)
		{
			nint num = queue.Dequeue();
			if (num == 0 || num == head || !hashSet.Add(num))
			{
				continue;
			}
			byte[] array = mem.TryReadBytes(num, 48);
			if (array != null)
			{
				queue.Enqueue((nint)BitConverter.ToInt64(array, 0));
				queue.Enqueue((nint)BitConverter.ToInt64(array, 16));
				nint num2 = (nint)BitConverter.ToInt64(array, 40);
				if (num2 != 0 && EntityReader.ReadMetadata(mem, num2).StartsWith("Metadata/", StringComparison.Ordinal))
				{
					return true;
				}
			}
		}
		return false;
	}

	private static int FindAreaLevelOffset(MemoryReader mem, nint area)
	{
		int[] array = new int[5] { 196, 192, 200, 188, 204 };
		foreach (int num in array)
		{
			int num2 = mem.ReadInt32(area + num);
			if (num2 >= 1 && num2 <= 100)
			{
				return num;
			}
		}
		return 196;
	}

	private static (int Owner, int AnchorRef, int AnchorHolder, int HoleCount, int AnchorPos, int ListenerSub) DiscoverRuneLayout(MemoryReader mem, nint area, int awakeOff)
	{
		return (Owner: 16, AnchorRef: 40, AnchorHolder: 48, HoleCount: 56, AnchorPos: 60, ListenerSub: 160);
	}
}
