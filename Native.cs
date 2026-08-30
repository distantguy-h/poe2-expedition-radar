using System;
using System.Runtime.InteropServices;

internal static class Native
{
	public struct MEMORY_BASIC_INFORMATION
	{
		public nint BaseAddress;

		public nint AllocationBase;

		public uint AllocationProtect;

		public ushort PartitionId;

		public ushort Alignment;

		public nuint RegionSize;

		public uint State;

		public uint Protect;

		public uint Type;
	}

	public const uint MEM_COMMIT = 4096u;

	public const uint MEM_IMAGE = 16777216u;

	public const uint PAGE_GUARD = 256u;

	public const uint PAGE_EXECUTE = 16u;

	public const uint PAGE_EXECUTE_READ = 32u;

	public const uint PAGE_EXECUTE_READWRITE = 64u;

	public const uint EXECUTE_MASK = 112u;

	[DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
	public static extern nint OpenProcess(uint access, [MarshalAs(UnmanagedType.Bool)] bool inherit, uint pid);

	[DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool CloseHandle(nint handle);

	[DllImport("ntdll.dll", ExactSpelling = true)]
	public static extern unsafe int NtReadVirtualMemory(nint process, nint address, void* buffer, nuint size, out nuint bytesRead);

	[DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
	public static extern unsafe nuint VirtualQueryEx(nint process, nint address, out MEMORY_BASIC_INFORMATION info, nuint length);
}
