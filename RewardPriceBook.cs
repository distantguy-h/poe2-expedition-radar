using System;
using System.Collections.Generic;
using System.Linq;

internal sealed class RewardPriceBook
{
	private readonly Dictionary<string, RewardPrice> _prices;

	public int Count => _prices.Count;

	public static RewardPriceBook Empty { get; } = new RewardPriceBook(Array.Empty<(string, double?, double?)>());

	public RewardPriceBook(IEnumerable<(string Name, double? Exalted, double? Divine)> values)
	{
		_prices = values.Where<(string, double?, double?)>(((string Name, double? Exalted, double? Divine) x) => !string.IsNullOrWhiteSpace(x.Name)).GroupBy<(string, double?, double?), string>(((string Name, double? Exalted, double? Divine) x) => x.Name.Trim(), StringComparer.OrdinalIgnoreCase).ToDictionary<IGrouping<string, (string, double?, double?)>, string, RewardPrice>((IGrouping<string, (string Name, double? Exalted, double? Divine)> x) => x.Key, (IGrouping<string, (string Name, double? Exalted, double? Divine)> x) => new RewardPrice(x.First().Exalted, x.First().Divine), StringComparer.OrdinalIgnoreCase);
	}

	public RewardPrice? Find(string rewardName)
	{
		if (!_prices.TryGetValue(rewardName.Trim(), out RewardPrice value))
		{
			return null;
		}
		return value;
	}
}
