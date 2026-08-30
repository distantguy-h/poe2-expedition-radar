using System;

internal sealed record LicenseSnapshot(LicenseState State, string Message, string? Plan = null, DateTimeOffset? ExpiresAt = null)
{
	public bool CanScan
	{
		get
		{
			LicenseState state = State;
			if ((uint)(state - 2) <= 2u)
			{
				return true;
			}
			return false;
		}
	}
}
