﻿using System.Diagnostics.CodeAnalysis;

namespace Signify.PAD.Svc.Core.ApiClient.Response;

[ExcludeFromCodeCoverage]
public class AccessToken
{
	public string access_token { get; set; }
	public int expires_in { get; set; }
}