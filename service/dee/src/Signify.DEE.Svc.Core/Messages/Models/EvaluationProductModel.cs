namespace Signify.DEE.Svc.Core.Messages.Models;

public class EvaluationProductModel
{
    public long EvaluationId { get; set; }
    public string ProductCode { get; set; }

    public override string ToString()
        => $"{nameof(EvaluationId)}: {EvaluationId}, {nameof(ProductCode)}: {ProductCode}";

    private bool Equals(EvaluationProductModel other)
        => EvaluationId == other.EvaluationId && string.Equals(ProductCode, other.ProductCode);

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((EvaluationProductModel) obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (EvaluationId.GetHashCode() * 397) ^ (ProductCode != null ? ProductCode.GetHashCode() : 0);
        }
    }
}