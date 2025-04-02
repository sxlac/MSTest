using Refit;
using System.Net.Http;
using System.Net;
using System;

namespace Signify.Spirometry.Core.Tests;

/// <summary>
/// Used for testing scenarios where an API returns a non-200-level HTTP status code
/// </summary>
public class FakeApiException : ApiException
{
    /// <param name="method">HTTP method used</param>
    /// <param name="statusCode">HTTP status code returned by the server</param>
    /// <param name="content">HTTP response content as a string</param>
    public FakeApiException(HttpMethod method, HttpStatusCode statusCode, string content = "")
        : base(new HttpRequestMessage(method, new Uri("http://localhost")), // any valid uri really
            method,
            content,
            statusCode,
            null,
            new HttpResponseMessage().Headers,
            new RefitSettings())
    {

    }
}