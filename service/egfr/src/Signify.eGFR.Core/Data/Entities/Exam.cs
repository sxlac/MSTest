using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Signify.eGFR.Core.Data.Entities;

/// <summary>
/// Details of a eGFR exam that a provider was scheduled to perform during an in-home health care visit.
/// Although a eGFR exam may have been scheduled, it does not necessarily mean that the exam was actually
/// performed during the appointment.
/// </summary>
[ExcludeFromCodeCoverage]
public class Exam
{
    /// <summary>
    /// Identifier of this eGFR exam
    /// </summary>
    [Key]
    public int ExamId { get; set; }
    /// <summary>
    /// Identifier of the application (source) that created this identifier
    /// </summary>
    public string ApplicationId { get; set; }
    /// <summary>
    /// Identifier of the evaluation from the Evaluation Service
    /// </summary>
    public long EvaluationId { get; set; }
    /// <summary>
    /// Identifier of the provider that performed the in-home evaluation
    /// </summary>
    public int ProviderId { get; set; }
    /// <summary>
    /// Identifier of the member (patient)
    /// </summary>
    public long MemberId { get; set; }
    /// <summary>
    /// Identifier of the member's healthcare plan
    /// </summary>
    public long MemberPlanId { get; set; }
    /// <summary>
    /// Although there is a <see cref="MemberId"/> identifier, this is yet another unique identifier
    /// for the member, which is from the CenseoNet DB.
    /// </summary>
    public string CenseoId { get; set; }
    /// <summary>
    /// Identifier of the in-home appointment with the member
    /// </summary>
    public long AppointmentId { get; set; }
    /// <summary>
    /// From the evaluation event, when the provider gave the service
    /// </summary>
    public DateTimeOffset? DateOfService { get; set; }
    /// <summary>
    /// Identifier of the client (ie insurance company)
    /// </summary>
    /// <remarks>
    /// Only null for records inserted before RCM integration; all new finalized evaluations will have this set
    /// </remarks>
    public int ClientId { get; set; }

    #region Member PII
    /// <summary>
    /// Member's first name
    /// </summary>
    public string FirstName { get; set; }
    /// <summary>
    /// Member's middle name
    /// </summary>
    public string MiddleName { get; set; }
    /// <summary>
    /// Member's last name
    /// </summary>
    public string LastName { get; set; }
    /// <summary>
    /// Member's date of birth
    /// </summary>
    public DateTime? DateOfBirth { get; set; }
    /// <summary>
    /// Member's physical address of residence
    /// </summary>
    public string AddressLineOne { get; set; }
    /// <summary>
    /// Member's physical address of residence (line 2)
    /// </summary>
    public string AddressLineTwo { get; set; }
    /// <summary>
    /// Member's physical city of residence
    /// </summary>
    public string City { get; set; }
    /// <summary>
    /// Member's physical US state of residence
    /// </summary>
    public string State { get; set; }
    /// <summary>
    /// Member's physical zip code of residence
    /// </summary>
    public string ZipCode { get; set; }
    #endregion Member PII

    /// <summary>
    /// The NPI is a HIPAA Administrative Simplification Standard. This is a unique 10-digit identification
    /// number for covered health care providers.
    ///
    /// For more information, see
    /// https://www.cms.gov/Regulations-and-Guidance/Administrative-Simplification/NationalProvIdentStand
    /// </summary>
    public string NationalProviderIdentifier { get; set; }

    /// <summary>
    /// From the evaluation event, when the evaluation was received by the Evaluation API
    /// </summary>
    public DateTimeOffset EvaluationReceivedDateTime { get; set; }
    /// <summary>
    /// From the evaluation event, when the evaluation was first started/created
    /// </summary>
    public DateTimeOffset EvaluationCreatedDateTime { get; set; }
    /// <summary>
    /// When the eGFR process manager received this event
    /// </summary>
    public DateTimeOffset CreatedDateTime { get; set; }

    public virtual ICollection<BillRequestSent> BillRequestSents { get; set; } = new List<BillRequestSent>();
    public virtual ExamNotPerformed ExamNotPerformed { get; set; }
    public virtual ICollection<BarcodeHistory> BarcodeHistories { get; set; } = new HashSet<BarcodeHistory>();
    public virtual ICollection<ExamStatus> ExamStatuses { get; set; } = new HashSet<ExamStatus>();
    public virtual LabResult LabResult { get; set; }
    public virtual ProviderPay ProviderPay { get; set; }
}