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
			if (array.Length != 2 || "KUxAMCcYjQffEL+s1CyFV7FUZ06Qr0+uPVTKPLFZ3b4=".StartsWith("REPLACE_"))
			{
				return false;
			}
			byte[] array2 = Decode(array[0]);
			byte[] signature = Decode(array[1]);
			byte[] publicKey = Convert.FromBase64String("KUxAMCcYjQffEL+s1CyFV7FUZ06Qr0+uPVTKPLFZ3b4=");
			if (!Ed25519.Verify(signature, array2, publicKey))
			{
				return false;
			}
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
