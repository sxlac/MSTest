using AutoMapper;
using Iris.Public.Types.Models;
using Iris.Public.Types.Models.Public._2._3._1;
using Iris.Public.Types.Models.V2_3_1;
using Signify.DEE.Messages;
using Signify.DEE.Messages.Status;
using Signify.DEE.Svc.Core.ApiClient.Requests;
using Signify.DEE.Svc.Core.Commands;
using Signify.DEE.Svc.Core.Constants;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.Events;
using Signify.DEE.Svc.Core.Events.Status;
using Signify.DEE.Svc.Core.Messages.Commands;
using Signify.DEE.Svc.Core.Messages.Models;
using Signify.DEE.Svc.Core.Messages.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using static Signify.DEE.Svc.Core.Constants.ApplicationConstants;
using BillRequestSent = Signify.DEE.Messages.Status.BillRequestSent;

namespace Signify.DEE.Svc.Core.Maps;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<PdfDeliveredToClient, ProcessPdfDelivered>();

        CreateMap<Exam, ExamModel>().ReverseMap();

        CreateMap<ExamImage, ExamImageModel>()
            .ForMember(dest => dest.ExamId, opt => opt.MapFrom(src => src.ExamId))
            .ForMember(dest => dest.ImageLocalId, opt => opt.MapFrom(src => src.ImageLocalId))
            .ForMember(dest => dest.Laterality, opt => opt.MapFrom(src => src.LateralityCodeId != null ? LateralityCode.All.Single(l => l.LateralityCodeId == src.LateralityCodeId.Value).Name : ""))
            .ForMember(dest => dest.NotGradableReasons, opt => opt.Ignore())
            .AfterMap((src, dest) =>
            {
                dest.NotGradableReasons = string.IsNullOrEmpty(src.NotGradableReasons)
                    ? new List<string>()
                    : src.NotGradableReasons.Split(NotGradableReasonsDelimiter).ToList();
            });

        CreateMap<ExamImageModel, ExamImage>()
            .ForMember(dest => dest.ImageLocalId, opt => opt.MapFrom(src => src.ImageLocalId))
            .ForMember(dest => dest.LateralityCode, opt => opt.MapFrom(src => !string.IsNullOrEmpty(src.Laterality) ? LateralityCode.Create(src.Laterality) : LateralityCode.Create(ApplicationConstants.Laterality.Unknown)))
            .ForMember(dest => dest.NotGradableReasons, opt => opt.MapFrom(src => string.Join(NotGradableReasonsDelimiter, src.NotGradableReasons)))
            .ForMember(dest => dest.ExamImageId, opt => opt.Ignore());

        CreateMap<PDFToClient, PdfToClientModel>().ReverseMap();
        CreateMap<ExamStatus, ExamStatusModel>().ReverseMap();
        CreateMap<ExamResult, ExamResultModel>().ReverseMap();
        CreateMap<ExamResultModel, ExamResult>()
            .ConvertUsing<ExamResultMapper>();

        CreateMap<RCMBillingRequestEvent, RCMBilling>();
        CreateMap<Exam, RCMBillingRequestEvent>()
            .ForMember(dest => dest.SharedClientId, mapper => mapper.MapFrom(src => src.ClientId == null ? 0 : src.ClientId))
            .ForMember(dest => dest.RcmProductCode, mapper => mapper.MapFrom(src => EvaluationObjective.GetProductBillingCode(src.EvaluationObjective.Objective)));

        CreateMap<ExamModel, RCMBilling>()
            .ForMember(dest => dest.UsStateOfService, mapper => mapper.MapFrom(src => src.State))
            .ForMember(dest => dest.RcmProductCode, mapper => mapper.MapFrom(src => EvaluationObjective.GetProductBillingCode(src.EvaluationObjective.Objective)));

        CreateMap<NotPerformedReason, DeeNotPerformed>()
            .ForMember(dest => dest.NotPerformedReasonId, mapper => mapper.MapFrom(src => src.NotPerformedReasonId));

        CreateMap<CreateIrisOrder, OrderRequest>()
            .ConvertUsing<IrisOrderMapper>();

        CreateMap<UploadIrisImages, ImageRequest>()
            .ConvertUsing<IrisImageUploadMapper>();

        #region Status events

        CreateMap<EvaluationFinalizedEvent, Performed>()
            .ForMember(dest => dest.ProductCode, mapper => mapper.MapFrom(_ => ProductCode))
            .ForMember(dest => dest.ReceivedDate, mapper => mapper.MapFrom(src => src.ReceivedDateTime));

        CreateMap<EvaluationFinalizedEvent, NotPerformed>()
            .ForMember(dest => dest.ProductCode, mapper => mapper.MapFrom(_ => ProductCode))
            .ForMember(dest => dest.ReceivedDate, mapper => mapper.MapFrom(src => src.ReceivedDateTime));

        CreateMap<ExamModel, Performed>()
            .ForMember(dest => dest.CreateDate, mapper => mapper.MapFrom(src => src.CreatedDateTime))
            .ForMember(dest => dest.RetinalImageTestingNotes, mapper => mapper.MapFrom(src => src.RetinalImageTestingNotes ?? string.Empty));

        CreateMap<ExamModel, NotPerformed>()
            .ForMember(dest => dest.CreateDate, mapper => mapper.MapFrom(src => src.CreatedDateTime))
            .ForMember(dest => dest.RetinalImageTestingNotes, mapper => mapper.MapFrom(src => src.RetinalImageTestingNotes ?? string.Empty));

        CreateMap<RCMBillingRequestEvent, BillRequestSent>()
            .ForMember(dest => dest.ProductCode, mapper => mapper.MapFrom(_ => ProductCode))
            .ForMember(dest => dest.PdfDeliveryDate, mapper => mapper.MapFrom(src => src.BillableDate))
            .ForMember(dest => dest.BillingProductCode, mapper => mapper.MapFrom(src => src.RcmProductCode));

        CreateMap<ExamModel, BillRequestNotSent>()
            .ForMember(dest => dest.ProductCode, mapper => mapper.MapFrom(_ => ProductCode))
            .ForMember(dest => dest.BillingProductCode, mapper => mapper.MapFrom(src => EvaluationObjective.GetProductBillingCode(src.EvaluationObjective.Objective)))
            .ForMember(dest => dest.CreateDate, mapper => mapper.MapFrom(src => src.CreatedDateTime));

        CreateMap<Exam, BillRequestNotSent>()
            .ForMember(dest => dest.ProductCode, mapper => mapper.MapFrom(_ => ProductCode))
            .ForMember(dest => dest.BillingProductCode, mapper => mapper.MapFrom(src => EvaluationObjective.GetProductBillingCode(src.EvaluationObjective.Objective)))
            .ForMember(dest => dest.CreateDate, mapper => mapper.MapFrom(src => src.CreatedDateTime));

        CreateMap<ExamModel, ResultsReceived>()
            .ForMember(dest => dest.ProductCode, mapper => mapper.MapFrom(_ => ProductCode))
            .ForMember(dest => dest.CreateDate, mapper => mapper.MapFrom(src => src.CreatedDateTime));

        CreateMap<NotPerformedModel, NotPerformed>();

        CreateMap<ProviderPayStatusEvent, ProviderPayRequestSent>()
            .ForMember(dest => dest.ProviderPayProductCode,
                mapper => mapper.MapFrom(src => src.ProductCode))
            .ForMember(dest => dest.ProductCode,
                mapper => mapper.MapFrom(_ => ProductCode))
            .ForMember(dest => dest.CreateDate, mapper => mapper.Ignore())
            .ForMember(dest => dest.ReceivedDate, mapper => mapper.MapFrom(src => src.ParentEventReceivedDateTime))
            .ForMember(dest => dest.PaymentId, mapper => mapper.MapFrom(src => src.PaymentId))
            .ForMember(dest => dest.ParentEventDateTime, mapper => mapper.MapFrom(src => src.StatusDateTime));

        CreateMap<ProviderPayStatusEvent, ProviderPayableEventReceived>()
            .ForMember(dest => dest.ProductCode, mapper => mapper.MapFrom(_ => ProductCode))
            .ForMember(dest => dest.CreateDate, mapper => mapper.Ignore())
            .ForMember(dest => dest.ReceivedDate, mapper => mapper.MapFrom(src => src.ParentEventReceivedDateTime));

        CreateMap<ProviderPayStatusEvent, ProviderNonPayableEventReceived>()
            .ForMember(dest => dest.ProductCode, mapper => mapper.MapFrom(_ => ProductCode))
            .ForMember(dest => dest.CreateDate, mapper => mapper.Ignore())
            .ForMember(dest => dest.ReceivedDate, mapper => mapper.MapFrom(src => src.ParentEventReceivedDateTime));

        CreateMap<ProviderPayRequest, ProviderPayStatusEvent>()
            .ForMember(dest => dest.ParentCdiEvent, mapper => mapper.MapFrom(src => src.ParentEvent))
            .ForMember(dest => dest.ProductCode, mapper => mapper.MapFrom(src => src.ProviderProductCode));

        CreateMap<ProviderPayRequest, GetMemberInfo>();
        CreateMap<CdiEventBase, ProviderPayStatusEvent>()
            .ForMember(dest => dest.EventId, mapper => mapper.MapFrom(src => src.RequestId))
            .ForMember(dest => dest.StatusDateTime, mapper => mapper.MapFrom(src => src.DateTime))
            .ForMember(dest => dest.ParentEventReceivedDateTime, mapper => mapper.MapFrom(src => src.ReceivedByDeeDateTime))
            .ForMember(dest => dest.ParentCdiEvent, mapper => mapper.MapFrom(src => src.GetType().Name));
        CreateMap<Exam, ProviderPayStatusEvent>()
            .ForMember(dest => dest.ProductCode, mapper => mapper.MapFrom(src => EvaluationObjective.GetProductBillingCode(src.EvaluationObjective.Objective)));
        CreateMap<ProviderPayRequest, SaveProviderPay>()
            .ForMember(dest => dest.ProviderPayProductCode, mapper => mapper.MapFrom(src => src.ProviderProductCode));
        CreateMap<SaveProviderPay, ProviderPayStatusEvent>()
            .ForMember(dest => dest.StatusDateTime, mapper => mapper.MapFrom(src => src.ParentEventDateTime))
            .ForMember(dest => dest.ProductCode, mapper => mapper.MapFrom(src => src.ProviderPayProductCode))
            .ForMember(dest => dest.ParentCdiEvent, mapper => mapper.MapFrom(src => src.ParentEvent));

        #endregion Status events

        CreateMap<Exam, Result>()
            .ConvertUsing<ResultMapper>();
        CreateMap<Exam, ICollection<SideResultInfo>>()
            .ConvertUsing<SideResultInfoMapper>();

        CreateMap<NotPerformedReason, NotPerformedModel>();

        CreateMap<NotPerformedModel, DeeNotPerformed>()
            .ForMember(dest => dest.Notes, mapper => mapper.MapFrom(src => src.ReasonNotes));

        CreateProcessIrisOrderResultMaps();
        CreateProviderPayRequestMaps();
        CreateHoldMaps();

        CreateMap<MemberModel, MemberInfoRs>();

        CreateMap<ProviderPayRequest, ProviderPayApiRequest>();
    }

    #region Private functions
    private void CreateHoldMaps()
    {
        CreateMap<CDIEvaluationHeldEvent, Hold>()
            .ConvertUsing<HoldMapper>();
    }

    private void CreateProviderPayRequestMaps()
    {
        CreateMap<Exam, ProviderPayRequest>()
            .ForMember(dest => dest.ProviderProductCode, mapper => mapper.MapFrom(src => EvaluationObjective.GetProductBillingCode(src.EvaluationObjective.Objective)))
            .ForMember(dest => dest.DateOfService, mapper => mapper.MapFrom(src => DateOnly.FromDateTime(src.DateOfService.Date).ToString("o")));
        CreateMap<ExamModel, ProviderPayRequest>()
            .ForMember(dest => dest.ProviderProductCode, mapper => mapper.MapFrom(src => EvaluationObjective.GetProductBillingCode(src.EvaluationObjective.Objective)))
            .ForMember(dest => dest.DateOfService, mapper => mapper.MapFrom(src => DateOnly.FromDateTime(src.DateOfService.Date).ToString("o")));
        CreateMap<ExamStatus, ProviderPayRequest>()
            .ForMember(dest => dest.ExamId, mapper => mapper.Ignore())
            .ForMember(dest => dest.EvaluationId, mapper => mapper.Ignore())
            .ForMember(dest => dest.ParentEventDateTime, mapper => mapper.MapFrom(src => src.CreatedDateTime))
            .ForMember(dest => dest.ParentEventReceivedDateTime, mapper => mapper.MapFrom(src => src.ReceivedDateTime))
            .ForMember(dest => dest.ParentEvent,
                mapper => mapper.MapFrom(src =>
                    src.ExamStatusCodeId == ExamStatusCode.CdiPassedReceived.ExamStatusCodeId ? nameof(CDIPassedEvent) : nameof(CDIFailedEvent)));
        CreateMap<CdiEventBase, ProviderPayRequest>()
            .ForMember(dest => dest.EventId, mapper => mapper.MapFrom(src => src.RequestId))
            .ForMember(dest => dest.ParentEventDateTime, mapper => mapper.MapFrom(src => src.DateTime))
            .ForMember(dest => dest.ParentEventReceivedDateTime, mapper => mapper.MapFrom(src => src.ReceivedByDeeDateTime))
            .ForMember(dest => dest.ParentEvent, mapper => mapper.MapFrom(src => src.GetType().Name));
    }
    private void CreateProcessIrisOrderResultMaps()
    {
        CreateMap<ResultGrading, ExamGraderModel>()
            .ForMember(dest => dest.FirstName, mapper => mapper.MapFrom(src => src.Provider.Name.First))
            .ForMember(dest => dest.LastName, mapper => mapper.MapFrom(src => src.Provider.Name.Last))
            .ForMember(dest => dest.NPI, mapper => mapper.MapFrom(src => src.Provider.NPI))
            .ForMember(dest => dest.Taxonomy, mapper => mapper.MapFrom(src => src.Provider.Taxonomy));
        CreateMap<ProcessIrisOrderResult, ExamResultModel>()
            .ForMember(dest => dest.ExamResultId, mapper => mapper.MapFrom(src => src.OrderResult.Order.PatientOrderID))
            .ForMember(dest => dest.ExamId, mapper => mapper.MapFrom(src => src.Exam.ExamId))
            .ForMember(dest => dest.GradableImage, mapper => mapper.MapFrom(src => src.OrderResult.ImageDetails.SingleEyeOnly ?
                (src.OrderResult.ImageDetails.LeftEyeCount > 0 || src.OrderResult.ImageDetails.RightEyeCount > 0) :
                (src.OrderResult.ImageDetails.LeftEyeCount > 0 && src.OrderResult.ImageDetails.RightEyeCount > 0)))
            .ForMember(dest => dest.CarePlan, mapper => mapper.MapFrom(src => src.OrderResult.Gradings.CarePlanName))
            .ForMember(dest => dest.DateSigned, mapper => mapper.MapFrom(src => src.OrderResult.Gradings.GradedTime.UtcDateTime))
            .ForMember(dest => dest.Diagnoses, mapper => mapper.MapFrom(src => src.OrderResult.Gradings.DiagnosisCodes.Select(x => x.Code).ToList()))
            .ForMember(dest => dest.LeftEyeHasPathology, mapper => mapper.MapFrom(src => DetermineEyePathology(src.OrderResult.Gradings.Pathology, src.OrderResult.Gradings.OS.Findings)))
            .ForMember(dest => dest.RightEyeHasPathology, mapper => mapper.MapFrom(src => DetermineEyePathology(src.OrderResult.Gradings.Pathology, src.OrderResult.Gradings.OD.Findings)))
            .ForMember(dest => dest.LeftEyeFindings, mapper => mapper.MapFrom(src => DetermineFinding(src.OrderResult.Gradings.OS.Findings)))
            .ForMember(dest => dest.RightEyeFindings, mapper => mapper.MapFrom(src => DetermineFinding(src.OrderResult.Gradings.OD.Findings)))
            .ForMember(d => d.Grader, opt => opt.MapFrom(s => s.OrderResult.Gradings))
            .ForMember(d => d.LeftEyeGradable, opt => opt.MapFrom(s => s.OrderResult.Gradings.OS.Gradable ?? true))
            .ForMember(d => d.RightEyeGradable, opt => opt.MapFrom(s => s.OrderResult.Gradings.OD.Gradable ?? true));
    }

    private static IEnumerable<string> DetermineFinding(IEnumerable<ResultFinding> eyeResultFindings)
    {
        var findings = new List<string>();
        if (eyeResultFindings != null)
        {
            foreach (var x in eyeResultFindings)
            {
                var finding = string.IsNullOrWhiteSpace(x.Result) ? $"Other - {x.Finding}" : $"{x.Finding} - {x.Result}";
                findings.Add(finding);
            }
        }

        return findings;
    }

    private static bool? DetermineEyePathology(bool pathology, IEnumerable<ResultFinding> eyeResultFindings)
    {
        if (eyeResultFindings is null)
        {
            return null;
        }

        if (!pathology)
        {
            return false;
        }

        foreach (var finding in eyeResultFindings)
        {
            if (Equals(finding.Finding, FindingNames.DiabeticRetinopathy) || Equals(finding.Finding, FindingNames.MacularEdema))
            {
                switch (finding.Result)
                {
                    case PathologyGradingResults.Positive:
                    case PathologyGradingResults.Mild:
                    case PathologyGradingResults.Severe:
                    case PathologyGradingResults.Moderate:
                    case PathologyGradingResults.Proliferative:
                        return true;
                    default:
                        break;
                }
            }
            else if (Equals(finding.Finding, FindingNames.WetAMD))
            {
                switch (finding.Result)
                {
                    case WetAMDFindingResult.Positive:
                        return true;
                    default:
                        break;
                }
            }
            else if (Equals(finding.Finding, FindingNames.DryAMD))
            {
                switch (finding.Result)
                {
                    case DryAMDFindingResult.EarlyStage:
                    case DryAMDFindingResult.IntermediateStage:
                    case DryAMDFindingResult.AdvAtrophicWithSubofealInvolvement:
                    case DryAMDFindingResult.AdvAtrophicWithoutSubofealInvolvement:
                        return true;
                    default:
                        break;
                }
            }
            else
            {
                return true; //Any other kind of finding other than 'Diabetic Retinopathy' or 'Macular Edema' is a pathology
            }
        }

        return false;
    }

    #endregion

    private static bool Equals(string finding, string search)
    {
        return string.Equals(finding, search, StringComparison.OrdinalIgnoreCase);
    }
}