using AutoMapper;
using Refit;
using Signify.PAD.Messages.Events;
using Signify.PAD.Svc.Core.ApiClient.Requests;
using Signify.PAD.Svc.Core.ApiClient.Response;
using Signify.PAD.Svc.Core.Commands;
using Signify.PAD.Svc.Core.Constants;
using Signify.PAD.Svc.Core.Data.Entities;
using Signify.PAD.Svc.Core.Events;
using Signify.PAD.Svc.Core.Events.Status;
using Signify.PAD.Svc.Core.Models;
using Signify.PAD.Svc.Core.Queries;
using System;
using Pad = Signify.PAD.Svc.Core.Data.Entities.PAD;
using ProviderPayRequest = Signify.PAD.Svc.Core.Commands.ProviderPayRequest;

namespace Signify.PAD.Svc.Core.Maps
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<EvaluationFinalizedEvent, EvalReceived>();
            CreateMap<EvaluationFinalizedEvent, CreatePad>();
            CreateMap<Pad, Performed>()
                .ForMember(dest => dest.CreateDate, mapper => mapper.MapFrom(src => src.CreatedDateTime))
                .ForMember(dest => dest.ReceivedDate, mapper => mapper.MapFrom(src => src.ReceivedDateTime))
                .ForMember(dest => dest.ProviderId, mapper => mapper.MapFrom(src => src.ProviderId))
                .ForMember(dest => dest.EvaluationId, mapper => mapper.MapFrom(src => src.EvaluationId))
                .ForMember(dest => dest.MemberPlanId, mapper => mapper.MapFrom(src => src.MemberPlanId))
                .ForMember(dest => dest.ProductCode, mapper => mapper.MapFrom(src => Application.ProductCode));
            CreateMap<Pad, Commands.NotPerformed>()
                .ForMember(dest => dest.CreateDate, mapper => mapper.MapFrom(src => src.CreatedDateTime))
                .ForMember(dest => dest.ReceivedDate, mapper => mapper.MapFrom(src => src.ReceivedDateTime))
                .ForMember(dest => dest.ProviderId, mapper => mapper.MapFrom(src => src.ProviderId))
                .ForMember(dest => dest.EvaluationId, mapper => mapper.MapFrom(src => src.EvaluationId))
                .ForMember(dest => dest.MemberPlanId, mapper => mapper.MapFrom(src => src.MemberPlanId))
                .ForMember(dest => dest.ProductCode, mapper => mapper.MapFrom(src => Application.ProductCode));
            CreateMap<PADStatus, BillRequestSent>()
                .ForMember(dest => dest.BillingProductCode, mapper => mapper.MapFrom(src => Application.ProductCode))
                .ForMember(dest => dest.CreateDate, mapper => mapper.MapFrom(src => src.CreatedDateTime))
                .ForMember(dest => dest.ProductCode, mapper => mapper.MapFrom(src => Application.ProductCode));

            CreateMap<Pad, ResultsReceived>()
                .ForMember(dest => dest.PerformedDate, mapper => mapper.MapFrom(src => src.CreatedDateTime))
                .ForMember(dest => dest.ReceivedDate, mapper => mapper.MapFrom(src => src.ReceivedDateTime))
                .ForMember(dest => dest.EvaluationId, mapper => mapper.MapFrom(src => src.EvaluationId));

            CreateMap<EvaluationAnswers, ResultsReceived>()
                .ConvertUsing<ResultsReceivedConverter>();

            CreateMap<EvaluationAnswers, CreateOrUpdatePAD>()
                .ForMember(dest => dest.LeftSeverityAnswerValue,
                    mapper => mapper.MapFrom(src => src.LeftSeverity))
                .ForMember(dest => dest.RightSeverityAnswerValue,
                    mapper => mapper.MapFrom(src => src.RightSeverity));

            CreateMap<CreatePad, EvalReceived>();
            CreateMap<EvalReceived, CreateOrUpdatePAD>();
            CreateMap<EvalReceived, GetMemberInfo>();
            CreateMap<MemberInfoRs, CreateOrUpdatePAD>();
            CreateMap<CreateOrUpdatePAD, Pad>();
            CreateMap<Pad, PADPerformed>()
                .ForMember(dest => dest.CorrelationId, mapper => mapper.MapFrom(src => Guid.NewGuid()));
            CreateMap<Pad, CreateOrUpdatePAD>();

            CreateMap<PADPerformed, Pad>();
            CreateMap<ApiResponse<MemberInfoRs>, MemberInfoRs>();

            CreateMap<PdfDeliveredToClient, CreateOrUpdatePDFToClient>()
                .ForMember(dest => dest.DeliveryCreatedDateTime, mapper => mapper.MapFrom(src => src.CreatedDateTime));
            CreateMap<CreateOrUpdatePDFToClient, PDFToClient>()
                .ForMember(dest => dest.CreatedDateTime, mapper => mapper.MapFrom(_ => DateTime.UtcNow));
            CreateMap<RcmBillingRequest, CreateBillRequest>();
            CreateMap<Pad, RcmBillingRequest>()
                .ForMember(dest => dest.ApplicationId, mapper => mapper.MapFrom(_ => "signify.pad.service"))
                .ForMember(dest => dest.RcmProductCode, mapper => mapper.MapFrom(_ => Application.ProductCode))
                .ForMember(dest => dest.UsStateOfService, mapper => mapper.MapFrom(src => src.State))
                .ForMember(dest => dest.SharedClientId, mapper => mapper.MapFrom(src => src.ClientId));
            CreateMap<Pad, ProviderPayRequest>()
                .ForMember(dest => dest.ProviderProductCode, mapper => mapper.MapFrom(value => Application.ProductCode))
                .ForMember(dest => dest.PersonId, mapper => mapper.MapFrom(src => src.CenseoId))
                .ForMember(dest => dest.DateOfService, mapper => mapper.MapFrom(src => DateOnly.FromDateTime((DateTime)src.DateOfService).ToString("o")));
            CreateMap<Pad, ProviderPayRequestSent>()
                .ForMember(dest => dest.ProviderPayProductCode, mapper => mapper.MapFrom(value => Application.ProductCode))
                .ForMember(dest => dest.CreateDate, mapper => mapper.MapFrom(src => src.CreatedDateTime.UtcDateTime))
                .ForMember(dest => dest.ReceivedDate, mapper => mapper.MapFrom(src => src.ReceivedDateTime))
                .ForMember(dest => dest.ProductCode, mapper => mapper.MapFrom(value => Application.ProductCode));
            CreateMap<ProviderPayRequest, ProviderPayApiRequest>()
                .ForMember(dest => dest.DateOfService, mapper => mapper.MapFrom(src => src.DateOfService.ToString()));
            CreateMap<ProcessPendingWaveform, WaveformDocument>()
                .ConvertUsing<WaveformDocumentMapper>();
            CreateMap<Pad, ProviderPayableEventReceived>()
                .ForMember(dest => dest.ReceivedDate, mapper => mapper.MapFrom(src => src.ReceivedDateTime))
                .ForMember(dest => dest.CreateDate, mapper => mapper.MapFrom(src => src.CreatedDateTime.UtcDateTime))
                .ForMember(dest => dest.ProductCode, mapper => mapper.MapFrom(src => Application.ProductCode));
            CreateMap<Pad, ProviderNonPayableEventReceived>()
                .ForMember(dest => dest.ReceivedDate, mapper => mapper.MapFrom(src => src.ReceivedDateTime))
                .ForMember(dest => dest.CreateDate, mapper => mapper.MapFrom(src => src.CreatedDateTime.UtcDateTime))
                .ForMember(dest => dest.ProductCode, mapper => mapper.MapFrom(src => Application.ProductCode));
            CreateMap<Pad, BillRequestSent>()
                .ForMember(dest => dest.ReceivedDate, mapper => mapper.MapFrom(src => src.ReceivedDateTime));
            CreateMap<AoeSymptomAnswers, AoeSymptomSupportResult>()
                .ForMember(dest => dest.FootPainRestingElevatedLateralityCodeId, mapper => mapper.MapFrom(src => src.LateralityCodeId))
                .ForMember(dest => dest.PedalPulseCodeId, mapper => mapper.MapFrom(src => src.PedalPulseCodeId));
            CreateMap<AoeSymptomAnswers, AoeResult>()
                .ConvertUsing<AoeResultConverter>();
            CreateMap<EvalReceived, AoeResult>()
                .ForMember(dest => dest.ReceivedDate, mapper => mapper.MapFrom(src => src.ReceivedDateTime));

            CreateStatusEventMaps();
        }

        private void CreateStatusEventMaps()
        {
            CreateMap<ExamStatusEventNew, PADStatus>()
                .ForMember(destination => destination.PADId,
                    mapper => mapper.MapFrom(source => source.Exam.PADId))
                .ForMember(destination => destination.PADStatusCodeId,
                    mapper => mapper.MapFrom(source => (int)source.StatusCode))
                .ForMember(destination => destination.CreatedDateTime,
                    mapper => mapper.MapFrom(source => source.StatusDateTime));

            CreateMap<Pad, BillRequestNotSent>()
                .ForMember(destination => destination.CreateDate,
                    mapper => mapper.MapFrom(source => source.CreatedDateTime))
                .ForMember(destination => destination.ReceivedDate,
                    mapper => mapper.MapFrom(source => source.ReceivedDateTime));

            CreateMap<PDFToClient, BillRequestNotSent>()
                .ForMember(destination => destination.PdfDeliveryDate,
                    mapper => mapper.MapFrom(source => source.DeliveryDateTime));
        }
    }
}
