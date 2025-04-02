using System.Diagnostics.CodeAnalysis;

namespace Signify.Spirometry.Core.ApiClients.CdiApi.Flags.Requests;

/// <remarks>
/// With the exception of lacking a FlagId property (because this will be set in the response
/// object, not the request object), this corresponds to the model defined at:
/// https://dev.azure.com/signifyhealth/HCC/_git/cdi?path=/legacy/api/cdiapi/src/CH.CDI.Core/Models/SystemFlag.cs
/// </remarks>
[ExcludeFromCodeCoverage]
public class PendingCdiSystemFlag
{
    /// <summary>
    /// Identifier of the question to associate this flag with
    /// </summary>
    public int QuestionId { get; set; }

    /// <summary>
    /// Identifier of the answer to associate this flag with
    /// </summary>
    public int AnswerId { get; set; }

    /// <summary>
    /// Text to be displayed to the provider in a clarification
    /// </summary>
    /// <remarks>
    /// Supports Markdown
    /// </remarks>
    public string Notes { get; set; }

    /// <summary>
    /// Consists of an object, serialized as JSON, that is parsed in the mobile IHE app
    /// to extract overread results. This is to avoid parsing these values out of the
    /// <see cref="Notes"/> property, which consists of Markdown text that can change
    /// via configuration.
    /// </summary>
    public string AdminNotes { get; set; }
}