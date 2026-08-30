using System;

internal sealed record RewardRow(DateTime SeenAt, string Expedition, long AddressValue, int AreaLevel, int RuneCount, string Anchor, int AnchorPosition, string Reward, int Quantity, int Slots, string Runes, double? PriceExalted, double? PriceDivine)
{
	public string Address => $"0x{AddressValue:X}";

	public string Time => SeenAt.ToString("HH:mm:ss");

	public double? Value { get; set; }

	public string ValueText
	{
		get
		{
			double? value = Value;
			if (value.HasValue)
			{
				double valueOrDefault = value.GetValueOrDefault();
				return valueOrDefault.ToString((valueOrDefault >= 1000.0) ? "N0" : "N2");
			}
			return "—";
		}
	}
}
