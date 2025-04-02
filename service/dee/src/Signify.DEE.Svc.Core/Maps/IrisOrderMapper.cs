using AutoMapper;
using Iris.Public.Types.Enums;
using Iris.Public.Types.Models;
using Iris.Public.Types.Models.V2_3_1;
using Signify.DEE.Svc.Core.Commands;
using Signify.DEE.Svc.Core.Configs;
using Signify.DEE.Svc.Core.Constants;
using System;

namespace Signify.DEE.Svc.Core.Maps;

public class IrisOrderMapper(IrisConfig irisConfig) : ITypeConverter<CreateIrisOrder, OrderRequest>
{
    public OrderRequest Convert(CreateIrisOrder source, OrderRequest destination, ResolutionContext context)
    {
        Gender parseGender(string gender)
        {
            switch (gender)
            {
                case "Male":
                case "male":
                case "M":
                case "m":
                    return Gender.M;
                case "Female":
                case "female":
                case "F":
                case "f":
                    return Gender.F;
                default:
                    return Gender.U;
            }
        }
        destination ??= new OrderRequest();
        destination.OrderControlCode = OrderControlCode.NW;
        destination.ClientGuid = irisConfig.ClientGuid;

        var irisNamePoco = new PersonName()
        {
            First = source.ExamAnswers.ProviderFirstName,
            Last = source.ExamAnswers.ProviderLastName
        };

        destination.CameraOperator = new RequestProvider()
        {
            Name = irisNamePoco,
            NPI = source.ExamAnswers.ProviderNpi,
            Email = source.ExamAnswers.ProviderEmail
        };

        destination.Order = new()
        {
            EvaluationTypes = new EvaluationType[] { EvaluationType.DR_AMD },
            ScheduledTime = source.Exam.DateOfService,
            CreatedTime = DateTime.UtcNow,
            LocalId = source.Exam.ExamLocalId,
            State = source.Exam.State,
            SingleEyeOnly = source.Exam.HasEnucleation.HasValue && source.Exam.HasEnucleation.Value,
            MissingEyeReason = source.Exam.HasEnucleation.HasValue && source.Exam.HasEnucleation.Value ? ApplicationConstants.Enucleation : null
        };
        destination.Patient = new()
        {
            LocalId = source.ExamAnswers.MemberPlanId.ToString(),
            Name = new()
            {
                First = source.ExamAnswers.MemberFirstName,
                Last = source.ExamAnswers.MemberLastName,
            },
            Dob = source.ExamAnswers.MemberBirthDate.ToString(),
            Gender = parseGender(source.ExamAnswers.MemberGender)
        };
        destination.Site = new()
        {
            LocalId = irisConfig.SiteLocalId
        };
        return destination;
    }
}