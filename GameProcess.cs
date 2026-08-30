using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

internal sealed class GameProcess : IDisposable
{
	public const uint PROCESS_VM_READ = 16u;

	public const uint PROCESS_QUERY_LIMITED_INFORMATION = 4096u;

	private static readonly string[] ProcessNames = new string[5] { "PathOfExile", "PathOfExileSteam", "PathOfExile_x64", "PathOfExile_KG", "PathOfExileEGS" };

	public nint Handle { get; }

	public int Pid { get; }

	public string Name { get; }

	public nint ModuleBase { get; }

	public uint ModuleSize { get; }

	private GameProcess(nint handle, int pid, string name, nint moduleBase, uint moduleSize)
	{
		Handle = handle;
		Pid = pid;
		Name = name;
		ModuleBase = moduleBase;
		ModuleSize = moduleSize;
	}

	public static GameProcess? Attach()
	{
		string[] processNames = ProcessNames;
		foreach (string text in processNames)
		{
			Process[] processesByName = Process.GetProcessesByName(text);
			foreach (Process process in processesByName)
			{
				try
				{
					nint num = Native.OpenProcess(4112u, inherit: false, (uint)process.Id);
					if (num != 0)
					{
						ProcessModule mainModule = process.MainModule;
						if (mainModule != null)
						{
							return new GameProcess(num, process.Id, text, mainModule.BaseAddress, (uint)mainModule.ModuleMemorySize);
						}
						Native.CloseHandle(num);
					}
				}
				catch
				{
				}
				finally
				{
					process.Dispose();
				}
			}
		}
		return null;
	}

	public static bool IsGameRunning()
	{
		string[] processNames = ProcessNames;
		for (int i = 0; i < processNames.Length; i++)
		{
			Process[] processesByName = Process.GetProcessesByName(processNames[i]);
			try
			{
				if (processesByName.Length != 0)
				{
					return true;
				}
			}
			finally
			{
				Process[] array = processesByName;
				for (int j = 0; j < array.Length; j++)
				{
					array[j].Dispose();
				}
			}
		}
		return false;
	}

	public IEnumerable<(nint Base, long Size)> ExecutableRegions()
	{
		nint addr = ModuleBase;
		nint end = ModuleBase + (nint)ModuleSize;
		Native.MEMORY_BASIC_INFORMATION mbi;
		while (addr < end && Native.VirtualQueryEx(Handle, addr, out mbi, (nuint)Marshal.SizeOf<Native.MEMORY_BASIC_INFORMATION>()) != 0)
		{
			if (mbi.State == 4096 && mbi.Type == 16777216 && (mbi.Protect & 0x100) == 0 && (mbi.Protect & 0x70) != 0)
			{
				yield return (Base: mbi.BaseAddress, Size: (long)mbi.RegionSize);
			}
			nint num = mbi.BaseAddress + (nint)mbi.RegionSize;
			if (num <= addr)
			{
				break;
			}
			addr = num;
		}
	}

	public void Dispose()
	{
		if (Handle != 0)
		{
			Native.CloseHandle(Handle);
		}
	}
}
