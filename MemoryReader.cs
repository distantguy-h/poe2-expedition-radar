using System;
using System.Text;

internal sealed class MemoryReader : IDisposable
{
	private readonly nint _handle;

	public MemoryReader(nint handle)
	{
		_handle = handle;
	}

	public unsafe bool TryRead<T>(nint address, out T value) where T : unmanaged
	{
		value = default(T);
		fixed (T* buffer = &value)
		{
			if (Native.NtReadVirtualMemory(_handle, address, buffer, (nuint)sizeof(T), out var bytesRead) == 0)
			{
				return bytesRead == (nuint)sizeof(T);
			}
			return false;
		}
	}

	public nint Ptr(nint address)
	{
		if (!this.TryRead<nint>(address, out nint value))
		{
			return 0;
		}
		ulong num = (ulong)value;
		if ((num >= 65536 && num <= 140737488355327L) || 1 == 0)
		{
			return value;
		}
		return 0;
	}

	public int ReadInt32(nint address)
	{
		if (!TryRead<int>(address, out var value))
		{
			return 0;
		}
		return value;
	}

	public unsafe byte[]? TryReadBytes(nint address, int size)
	{
		if (size <= 0 || size > 67108864)
		{
			return null;
		}
		byte[] array = new byte[size];
		fixed (byte* buffer = array)
		{
			if (Native.NtReadVirtualMemory(_handle, address, buffer, (nuint)size, out var bytesRead) != 0 || bytesRead != (nuint)size)
			{
				return null;
			}
			return array;
		}
	}

	public string ReadUtf8(nint address, int maxBytes = 128)
	{
		byte[] array = TryReadBytes(address, maxBytes);
		if (array == null)
		{
			return "";
		}
		int num = Array.IndexOf(array, (byte)0);
		if (num < 0)
		{
			num = array.Length;
		}
		return Encoding.UTF8.GetString(array, 0, num);
	}

	public string ReadUtf16(nint address, int maxChars = 1024)
	{
		byte[] array = TryReadBytes(address, maxChars * 2);
		if (array == null)
		{
			return "";
		}
		int count = array.Length;
		for (int i = 0; i + 1 < array.Length; i += 2)
		{
			if (array[i] == 0 && array[i + 1] == 0)
			{
				count = i;
				break;
			}
		}
		return Encoding.Unicode.GetString(array, 0, count);
	}

	public string ReadStdWString(nint address)
	{
		if (!TryRead<int>(address + 16, out var value) || value <= 0 || value > 1024)
		{
			return "";
		}
		if (value < 8)
		{
			return ReadUtf16(address, value);
		}
		nint num = Ptr(address);
		if (num != 0)
		{
			return ReadUtf16(num, value);
		}
		return "";
	}

	public void Dispose()
	{
	}
}
