using System;

namespace Signify.eGFR.Core.Exceptions;

[Serializable]
public class ExamNotFoundException : Exception
{
    public string CenseoId { get; }
    public DateTimeOffset? CollectionDate { get; }

    /// <summary>
    /// Exam not found in DB
    /// </summary>
    /// <param name="censeoId"></param>
    /// <param name="collectionDate"></param>
    public ExamNotFoundException(String censeoId, DateTimeOffset? collectionDate)
        : base($"Exam with CenseoId:{censeoId} and CollectionDate:{collectionDate} not found in DB")
    {
        CenseoId = censeoId;
        CollectionDate = collectionDate;
    }
}