using System;
using System.Collections.Generic;

internal static class EntityReader
{
	public static List<EntityInfo> ReadAwakeEntities(MemoryReader mem, nint area, int awakeOffset)
	{
		List<EntityInfo> list = new List<EntityInfo>();
		nint num = mem.Ptr(area + awakeOffset);
		if (num == 0)
		{
			return list;
		}
		nint num2 = mem.Ptr(num + 8);
		if (num2 == 0)
		{
			return list;
		}
		Queue<nint> queue = new Queue<nint>();
		HashSet<nint> hashSet = new HashSet<nint>();
		queue.Enqueue(num2);
		while (queue.Count > 0 && hashSet.Count < 200000)
		{
			nint num3 = queue.Dequeue();
			if (num3 == 0 || num3 == num || !hashSet.Add(num3))
			{
				continue;
			}
			byte[] array = mem.TryReadBytes(num3, 48);
			if (array == null)
			{
				continue;
			}
			queue.Enqueue((nint)BitConverter.ToInt64(array, 0));
			queue.Enqueue((nint)BitConverter.ToInt64(array, 16));
			uint num4 = BitConverter.ToUInt32(array, 32);
			nint num5 = (nint)BitConverter.ToInt64(array, 40);
			if (num5 != 0 && num4 < 1073741824)
			{
				string text = ReadMetadata(mem, num5);
				if (!string.IsNullOrEmpty(text))
				{
					list.Add(new EntityInfo(num4, num5, text));
				}
			}
		}
		return list;
	}

	public static string ReadMetadata(MemoryReader mem, nint entity)
	{
		nint num = mem.Ptr(entity + 8);
		if (num == 0)
		{
			return "";
		}
		return mem.ReadStdWString(num + 8);
	}

	public static nint ResolveComponent(MemoryReader mem, nint entity, string wanted)
	{
		nint num = mem.Ptr(entity + 8);
		if (num == 0)
		{
			return 0;
		}
		nint num2 = mem.Ptr(num + 40);
		nint num3 = mem.Ptr(entity + 16);
		nint num4 = mem.Ptr(entity + 16 + 8);
		if (num2 == 0 || num3 == 0 || num4 == 0)
		{
			return 0;
		}
		long num5 = ((long)num4 - (long)num3) / 8;
		if (num5 <= 0 || num5 > 512)
		{
			return 0;
		}
		nint num6 = num2 + 40;
		nint num7 = mem.Ptr(num6);
		nint num8 = mem.Ptr(num6 + 8);
		if (num7 == 0 || num8 == 0)
		{
			return 0;
		}
		long num9 = ((long)num8 - (long)num7) / 16;
		if (num9 <= 0 || num9 > 512)
		{
			return 0;
		}
		for (long num10 = 0L; num10 < num9; num10++)
		{
			nint num11 = num7 + (nint)(num10 * 16);
			nint address = mem.Ptr(num11);
			if (mem.TryRead<int>(num11 + 8, out var value) && value >= 0 && value < num5 && string.Equals(mem.ReadUtf8(address, 96), wanted, StringComparison.Ordinal))
			{
				return mem.Ptr(num3 + value * 8);
			}
		}
		return 0;
	}
}
