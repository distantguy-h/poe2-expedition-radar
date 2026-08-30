using System.Collections.Generic;

internal static class MonolithReader
{
	public static ExpeditionState Read(MemoryReader mem, nint device, RuntimeOffsets o, IReadOnlyDictionary<int, string> runeNames)
	{
		nint num = EntityReader.ResolveComponent(mem, device, "StateMachine");
		if (num == 0)
		{
			return default(ExpeditionState);
		}
		nint num2 = mem.Ptr(num + o.StateMachineListener);
		nint num3 = mem.Ptr(num + o.StateMachineListener + 8);
		if (num2 == 0 || num3 == 0)
		{
			return default(ExpeditionState);
		}
		long num4 = ((long)num3 - (long)num2) / 8;
		if (num4 <= 0 || num4 > 256)
		{
			return default(ExpeditionState);
		}
		nint num5 = 0;
		for (long num6 = 0L; num6 < num4; num6++)
		{
			nint num7 = mem.Ptr(num2 + (nint)(num6 * 8));
			if (num7 == 0)
			{
				continue;
			}
			nint num8 = mem.Ptr(num7);
			if (num8 != 0)
			{
				nint num9 = num8 - o.RuneListenerSub;
				if (mem.Ptr(num9 + o.RuneOwner) == device)
				{
					num5 = num9;
					break;
				}
			}
		}
		if (num5 == 0)
		{
			return default(ExpeditionState);
		}
		int value;
		bool flag = !mem.TryRead<int>(num5 + o.RuneHoleCount, out value);
		if (!flag)
		{
			bool flag2 = ((value < 1 || value > 16) ? true : false);
			flag = flag2;
		}
		if (flag)
		{
			return default(ExpeditionState);
		}
		mem.TryRead<int>(num5 + o.RuneAnchorPos, out var value2);
		nint num10 = mem.Ptr(num5 + o.RuneAnchorRef);
		if (num10 == 0)
		{
			return new ExpeditionState(Resolved: true, value, -1, value2, IsUnique: true, Collected: false, "Unique / anchor-less");
		}
		nint num11 = mem.Ptr(num5 + o.RuneAnchorHolder);
		if (num11 == 0)
		{
			return new ExpeditionState(Resolved: true, value, -1, value2, IsUnique: false, Collected: false, "Unknown");
		}
		nint num12 = mem.Ptr(num11 + 40);
		if (num12 == 0)
		{
			return new ExpeditionState(Resolved: true, value, -1, value2, IsUnique: false, Collected: false, "Unknown");
		}
		nint num13 = mem.Ptr(num12);
		if (num13 == 0)
		{
			return new ExpeditionState(Resolved: true, value, -1, value2, IsUnique: false, Collected: false, "Unknown");
		}
		long num14 = (long)num10 - (long)num13;
		int num15 = (int)((num14 >= 0 && num14 % o.RuneStride == 0L) ? (num14 / o.RuneStride) : (-1));
		if (num15 < 0 || num15 >= o.RuneCount)
		{
			num15 = -1;
		}
		runeNames.TryGetValue(num15, out string value3);
		return new ExpeditionState(Resolved: true, value, num15, value2, IsUnique: false, Collected: false, value3 ?? $"Rune#{num15}");
	}
}
