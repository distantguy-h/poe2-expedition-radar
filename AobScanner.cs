using System;
using System.Collections.Generic;

internal static class AobScanner
{
	public static IEnumerable<nint> ScanForResolvedAddresses(GameProcess process, MemoryReader mem, Aob.Pattern pattern)
	{
		foreach (var region in process.ExecutableRegions())
		{
			byte[] bytes = mem.TryReadBytes(region.Base, checked((int)region.Size));
			if (bytes == null)
			{
				continue;
			}
			for (int i = 0; i <= bytes.Length - pattern.Bytes.Length; i++)
			{
				bool flag = true;
				for (int j = 0; j < pattern.Bytes.Length; j++)
				{
					byte? b = pattern.Bytes[j];
					if (b.HasValue)
					{
						byte valueOrDefault = b.GetValueOrDefault();
						if (bytes[i + j] != valueOrDefault)
						{
							flag = false;
							break;
						}
					}
				}
				if (flag)
				{
					int num = BitConverter.ToInt32(bytes, i + pattern.DispOffset);
					nint num2 = region.Base + i + pattern.InstrLen;
					yield return num2 + num;
				}
			}
		}
	}
}
