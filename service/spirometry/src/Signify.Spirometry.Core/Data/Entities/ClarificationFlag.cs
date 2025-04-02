using System;
using System.Diagnostics.CodeAnalysis;

namespace Signify.Spirometry.Core.Data.Entities;

/// <summary>
/// Details of a clarification flag that can be sent to the provider that
/// performed the evaluation
/// </summary>
[ExcludeFromCodeCoverage]
public class ClarificationFlag
{
    /// <summary>
    /// PK identifier of this clarification flag in the Spirometry database
    /// </summary>
    public int ClarificationFlagId { get; set; }
    /// <summary>
    /// Identifier of the spirometry exam associated with this flag
    /// </summary>
    public int SpirometryExamId { get; set; }
    /// <summary>
    /// Identifier of this flag within the context of the CDI domain
    /// </summary>
    public int CdiFlagId { get; set; }
    /// <summary>
    /// When this flag was created
    /// </summary>
    public DateTime CreateDateTime { get; set; }

    public virtual SpirometryExam SpirometryExam { get; set; }
}