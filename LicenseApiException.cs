using System;
using System.Net;

internal sealed class LicenseApiException(string code, HttpStatusCode status) : Exception(code)
{
	public string Code { get; } = code;

	public HttpStatusCode Status { get; } = status;
}
