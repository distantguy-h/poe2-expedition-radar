using System;

internal sealed class DataDisabledException : Exception
{
	public DataDisabledException(string message)
		: base(message)
	{
	}
}
