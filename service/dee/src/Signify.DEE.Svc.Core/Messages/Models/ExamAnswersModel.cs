using Signify.DEE.Svc.Core.ApiClient.Responses;
using System;
using System.Collections.Generic;

namespace Signify.DEE.Svc.Core.Messages.Models;

public class ExamAnswersModel
{
    public DateTimeOffset DateOfService { get; set; }
    public string State { get; set; }
    public long MemberPlanId { get; set; }
    public string MemberFirstName { get; set; }
    public string MemberLastName { get; set; }
    public string MemberGender { get; set; }
    public DateTime MemberBirthDate { get; set; }
    public string ProviderId { get; set; }
    public string ProviderFirstName { get; set; }
    public string ProviderLastName { get; set; }
    public string ProviderNpi { get; set; }
    public string ProviderEmail { get; set; }
    public List<string> Images { get; set; } = new();
    public DateTimeOffset CreatedDateTime { get; set; }

    public List<EvaluationAnswer> Answers { get; set; }
    public string RetinalImageTestingNotes { get; set; }
    public bool? HasEnucleation { get; set; }
    public override string ToString()
    {
        return $"{nameof(DateOfService)}: {DateOfService}, {nameof(State)}: {State}, {nameof(MemberPlanId)}: {MemberPlanId}, {nameof(MemberFirstName)}: {MemberFirstName}, {nameof(MemberLastName)}: {MemberLastName}, {nameof(MemberGender)}: {MemberGender}, {nameof(MemberBirthDate)}: {MemberBirthDate}, {nameof(ProviderId)}: {ProviderId}, {nameof(ProviderFirstName)}: {ProviderFirstName}, {nameof(ProviderLastName)}: {ProviderLastName}, {nameof(ProviderNpi)}: {ProviderNpi}, {nameof(ProviderEmail)}: {ProviderEmail}, {nameof(Images)}: {Images}, {nameof(CreatedDateTime)}: {CreatedDateTime}, {nameof(Answers)}: {Answers}";
    }

    private bool Equals(ExamAnswersModel other)
    {
        return DateOfService.Equals(other.DateOfService) && State == other.State && MemberPlanId == other.MemberPlanId && MemberFirstName == other.MemberFirstName && MemberLastName == other.MemberLastName && MemberGender == other.MemberGender && MemberBirthDate.Equals(other.MemberBirthDate) && ProviderId == other.ProviderId && ProviderFirstName == other.ProviderFirstName && ProviderLastName == other.ProviderLastName && ProviderNpi == other.ProviderNpi && ProviderEmail == other.ProviderEmail && Equals(Images, other.Images) && CreatedDateTime.Equals(other.CreatedDateTime) && Equals(Answers, other.Answers);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((ExamAnswersModel) obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = DateOfService.GetHashCode();
            hashCode = (hashCode * 397) ^ (State != null ? State.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ MemberPlanId.GetHashCode();
            hashCode = (hashCode * 397) ^ (MemberFirstName != null ? MemberFirstName.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (MemberLastName != null ? MemberLastName.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (MemberGender != null ? MemberGender.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ MemberBirthDate.GetHashCode();
            hashCode = (hashCode * 397) ^ (ProviderId != null ? ProviderId.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (ProviderFirstName != null ? ProviderFirstName.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (ProviderLastName != null ? ProviderLastName.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (ProviderNpi != null ? ProviderNpi.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (ProviderEmail != null ? ProviderEmail.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (Images != null ? Images.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ CreatedDateTime.GetHashCode();
            hashCode = (hashCode * 397) ^ (Answers != null ? Answers.GetHashCode() : 0);
            return hashCode;
        }
    }
}