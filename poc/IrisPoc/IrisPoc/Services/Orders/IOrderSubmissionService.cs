using IrisPoc.Models;

namespace IrisPoc.Services.Orders;

public interface IOrderSubmissionService
{
    /// <summary>
    /// Submit the order to Iris
    /// </summary>
    Task SubmitRequest(ExamModel exam, CancellationToken cancellationToken);
}
