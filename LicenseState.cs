internal enum LicenseState
{
	Uninitialized,
	Activating,
	ValidOnline,
	ValidCached,
	RefreshPending,
	Expired,
	Revoked,
	DeviceMismatch,
	ServerUnavailable,
	Invalid
}
