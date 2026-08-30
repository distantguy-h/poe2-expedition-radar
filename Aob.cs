internal static class Aob
{
	public sealed record Pattern(byte?[] Bytes, int DispOffset, int InstrLen);

	public static readonly Pattern[] GameStateRefs = new Pattern[2]
	{
		new Pattern(new byte?[13]
		{
			(byte)72,
			(byte)57,
			(byte)45,
			null,
			null,
			null,
			null,
			(byte)15,
			(byte)133,
			(byte)22,
			(byte)1,
			0,
			0
		}, 3, 7),
		new Pattern(new byte?[13]
		{
			(byte)72,
			(byte)57,
			(byte)45,
			null,
			null,
			null,
			null,
			(byte)15,
			(byte)133,
			null,
			null,
			null,
			null
		}, 3, 7)
	};
}
