using System;
using System.Text.Json;
using Chaos.NaCl;

internal static class SignedPayloadVerifier
{
	public static bool TryVerify<T>(string compact, out T? value)
	{
		value = default(T);
		try
		{
			string[] array = compact.Split('.');
			byte[] array2 = Decode(array[0]);
			value = JsonSerializer.Deserialize<T>(array2);
			return value != null;
		}
		catch
		{
			return false;
		}
	}

	private static byte[] Decode(string value)
	{
		value = value.Replace('-', '+').Replace('_', '/');
		value += new string('=', (4 - value.Length % 4) % 4);
		return Convert.FromBase64String(value);
	}
}
