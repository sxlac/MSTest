using System;

namespace Signify.DEE.Svc.Core.Messages.Models;

public class EvaluationDocumentModel
{
    public long EvaluationId { get; set; }
    public string DocumentType { get; set; }
    public int Version { get; set; }
    public string FilePath { get; set; }
    public DateTimeOffset CreatedDateTime { get; set; }
    public string UserName { get; set; }

    public override string ToString()
    {
        return $"{nameof(EvaluationId)}: {EvaluationId}, {nameof(DocumentType)}: {DocumentType}, {nameof(Version)}: {Version}, {nameof(FilePath)}: {FilePath}, {nameof(CreatedDateTime)}: {CreatedDateTime}, {nameof(UserName)}: {UserName}";
    }

    private bool Equals(EvaluationDocumentModel other)
    {
        return EvaluationId == other.EvaluationId && string.Equals(DocumentType, other.DocumentType) && Version == other.Version && string.Equals(FilePath, other.FilePath) && CreatedDateTime.Equals(other.CreatedDateTime) && string.Equals(UserName, other.UserName);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == this.GetType() && Equals((EvaluationDocumentModel)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = EvaluationId.GetHashCode();
            hashCode = (hashCode * 397) ^ (DocumentType != null ? DocumentType.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ Version;
            hashCode = (hashCode * 397) ^ (FilePath != null ? FilePath.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ CreatedDateTime.GetHashCode();
            hashCode = (hashCode * 397) ^ (UserName != null ? UserName.GetHashCode() : 0);
            return hashCode;
        }
    }
}