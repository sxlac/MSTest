using System;
using System.Diagnostics.CodeAnalysis;

namespace Signify.PAD.Svc.Core.Models;

[ExcludeFromCodeCoverage]
public sealed class EvaluationDocumentModel: IEquatable<EvaluationDocumentModel>
{
    public int EvaluationId { get; set; }
    public string DocumentType { get; set; }
    public int Version { get; set; }
    public string FilePath { get; set; }
    public DateTime CreatedDateTime { get; set; }
    public string UserName { get; set; }

    public override string ToString()
        => $"{nameof(EvaluationId)}: {EvaluationId}, {nameof(DocumentType)}: {DocumentType}, {nameof(Version)}: {Version}, {nameof(FilePath)}: {FilePath}, {nameof(CreatedDateTime)}: {CreatedDateTime}, {nameof(UserName)}: {UserName}";
        

    #region IEquatable
    public bool Equals(EvaluationDocumentModel other)
    {
        return EvaluationId == other.EvaluationId && string.Equals(DocumentType, other.DocumentType) && Version == other.Version && string.Equals(FilePath, other.FilePath) && CreatedDateTime.Equals(other.CreatedDateTime) && string.Equals(UserName, other.UserName);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((EvaluationDocumentModel) obj);
    }
        
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = EvaluationId;
            hashCode = (hashCode * 397) ^ (DocumentType != null ? DocumentType.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ Version;
            hashCode = (hashCode * 397) ^ (FilePath != null ? FilePath.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ CreatedDateTime.GetHashCode();
            hashCode = (hashCode * 397) ^ (UserName != null ? UserName.GetHashCode() : 0);
            return hashCode;
        }
    }
    #endregion IEquatable
}