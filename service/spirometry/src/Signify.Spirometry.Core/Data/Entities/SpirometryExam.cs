﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace Signify.Spirometry.Core.Data.Entities;

/// <summary>
/// Details of a Spirometry exam that a provider was scheduled to perform during an in-home health care visit.
/// Although a Spirometry exam may have been scheduled, it does not necessarily mean that the exam was actually
/// performed during the appointment.
/// </summary>
[ExcludeFromCodeCoverage]
#pragma warning disable S4035 // SonarQube - Classes implementing "IEquatable<T>" should be sealed - Cannot seal because EF extends this class (see virtual members)
public class SpirometryExam : IEquatable<SpirometryExam>
#pragma warning restore S4035
{
    /// <summary>
    /// Identifier of this Spirometry exam
    /// </summary>
    [Key]
    public int SpirometryExamId { get; set; }
    /// <summary>
    /// Identifier of the application (source) that created this identifier
    /// </summary>
    public string ApplicationId { get; set; }
    /// <summary>
    /// Identifier of the evaluation from the Evaluation Service
    /// </summary>
    public int EvaluationId { get; set; }
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
    public int MemberPlanId { get; set; }
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
    public DateTime? DateOfService { get; set; }
    /// <summary>
    /// Identifier of the client (ie insurance company)
    /// </summary>
    /// <remarks>
    /// Only null for records inserted before RCM integration; all new finalized evaluations will have this set
    /// </remarks>
    public int? ClientId { get; set; }
    /// <summary>
    /// Version of the Form this evaluation corresponds to.
    /// </summary>
    public int? FormVersionId { get; set; }

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
    public DateTime EvaluationReceivedDateTime { get; set; }
    /// <summary>
    /// From the evaluation event, when the evaluation was first started/created
    /// </summary>
    public DateTime EvaluationCreatedDateTime { get; set; }
    /// <summary>
    /// When the Spirometry process manager received this event
    /// </summary>
    public DateTime CreatedDateTime { get; set; }

    #region Equality
    public bool Equals(SpirometryExam other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;

        return SpirometryExamId == other.SpirometryExamId
               && ApplicationId == other.ApplicationId
               && EvaluationId == other.EvaluationId
               && ProviderId == other.ProviderId
               && MemberId == other.MemberId
               && MemberPlanId == other.MemberPlanId
               && CenseoId == other.CenseoId
               && AppointmentId == other.AppointmentId
               && Nullable.Equals(DateOfService, other.DateOfService)
               && Nullable.Equals(ClientId, other.ClientId)
               && Nullable.Equals(FormVersionId, other.FormVersionId)
               && FirstName == other.FirstName
               && MiddleName == other.MiddleName
               && LastName == other.LastName
               && Nullable.Equals(DateOfBirth, other.DateOfBirth)
               && AddressLineOne == other.AddressLineOne
               && AddressLineTwo == other.AddressLineTwo
               && City == other.City
               && State == other.State
               && ZipCode == other.ZipCode
               && NationalProviderIdentifier == other.NationalProviderIdentifier
               && EvaluationReceivedDateTime.Equals(other.EvaluationReceivedDateTime)
               && EvaluationCreatedDateTime.Equals(other.EvaluationCreatedDateTime)
               && CreatedDateTime.Equals(other.CreatedDateTime);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((SpirometryExam) obj);
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(SpirometryExamId);
        hashCode.Add(ApplicationId);
        hashCode.Add(EvaluationId);
        hashCode.Add(ProviderId);
        hashCode.Add(MemberId);
        hashCode.Add(MemberPlanId);
        hashCode.Add(CenseoId);
        hashCode.Add(AppointmentId);
        hashCode.Add(DateOfService);
        hashCode.Add(ClientId);
        hashCode.Add(FormVersionId);
        hashCode.Add(FirstName);
        hashCode.Add(MiddleName);
        hashCode.Add(LastName);
        hashCode.Add(DateOfBirth);
        hashCode.Add(AddressLineOne);
        hashCode.Add(AddressLineTwo);
        hashCode.Add(City);
        hashCode.Add(State);
        hashCode.Add(ZipCode);
        hashCode.Add(NationalProviderIdentifier);
        hashCode.Add(EvaluationReceivedDateTime);
        hashCode.Add(EvaluationCreatedDateTime);
        hashCode.Add(CreatedDateTime);
        return hashCode.ToHashCode();
    }

    public static bool operator ==(SpirometryExam left, SpirometryExam right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(SpirometryExam left, SpirometryExam right)
    {
        return !Equals(left, right);
    }
    #endregion Equality

    public virtual ICollection<BillRequestSent> BillRequestSents { get; set; } = new List<BillRequestSent>();
    public virtual ClarificationFlag ClarificationFlag { get; set; }
    public virtual ExamNotPerformed ExamNotPerformed { get; set; }
    public virtual ICollection<ExamStatus> ExamStatuses { get; set; } = new HashSet<ExamStatus>();
    public virtual ProviderPay ProviderPay { get; set; }
    public virtual SpirometryExamResult SpirometryExamResult { get; set; }
}