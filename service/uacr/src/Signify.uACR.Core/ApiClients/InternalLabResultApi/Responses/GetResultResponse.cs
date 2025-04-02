using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Signify.uACR.Core.ApiClients.InternalLabResultApi.Responses;

/// <summary>
/// Get Internal Lab Result Response
/// </summary>
[ExcludeFromCodeCoverage]
public class GetResultResponse
{
    /// <summary>
    /// Lab Result Id
    /// </summary>
    [Required]
    public int LabResultId { get; set; }
    /// <summary>
    /// Request Id
    /// </summary>
    public Guid RequestId { get; set; }
    /// <summary>
    /// Vendor Name
    /// </summary>
    [Required]
    public string VendorName { get; set; }
    /// <summary>
    /// Test Names
    /// </summary>
    [Required]
    public ISet<string> TestNames { get; set; }
    /// <summary>
    /// Received Date Time
    /// </summary>
    [Required]
    public DateTimeOffset ReceivedDateTime { get; set; }
    /// <summary>
    /// Meta Data
    /// </summary>
    public JsonElement? MetaData { get; set; }
    /// <summary>
    /// Vendor Data
    /// </summary>
    [Required]
    public JsonElement VendorData { get; set; }
}