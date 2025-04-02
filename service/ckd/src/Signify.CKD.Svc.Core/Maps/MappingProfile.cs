using AutoMapper;
using Refit;
using Signify.CKD.Messages.Events;
using Signify.CKD.Sagas;
using Signify.CKD.Svc.Core.ApiClient.Requests;
using Signify.CKD.Svc.Core.ApiClient.Response;
using Signify.CKD.Svc.Core.Commands;
using Signify.CKD.Svc.Core.Constants;
using Signify.CKD.Svc.Core.Data.Entities;
using Signify.CKD.Svc.Core.Events;
using Signify.CKD.Svc.Core.Messages.Status;
using Signify.CKD.Svc.Core.Models;
using Signify.CKD.Svc.Core.Queries;
using Signify.CKD.Svc.Core.Sagas.Commands;
using System;
using Result = Signify.CKD.Svc.Core.Messages.Result;

namespace Signify.CKD.Svc.Core.Maps;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<EvaluationFinalizedEvent, EvalReceived>();
        CreateMap<EvaluationAnswers, CreateOrUpdateCKD>()
            .ForMember(dest => dest.CKDAnswer, mapper => mapper.MapFrom(src => src.LookupCKDAnswerEntity != null ? src.LookupCKDAnswerEntity.CKDAnswerValue : null));
        CreateMap<EvalReceived, CreateOrUpdateCKD>();
        CreateMap<EvalReceived, GetMemberInfo>();
        CreateMap<MemberInfoRs, CreateOrUpdateCKD>();
        CreateMap<CreateOrUpdateCKD, Data.Entities.CKD>().ReverseMap();
        CreateMap<Data.Entities.CKD, CKDPerformed>()
            .ForMember(dest => dest.CorrelationId, mapper => mapper.MapFrom(src => Guid.NewGuid()));

        CreateMap<CKDPerformed, Data.Entities.CKD>();

        CreateMap<CKDPerformed, UpdateInventory>();
        CreateMap<UpdateInventory, Data.Entities.CKD>();

        CreateMap<ApiResponse<MemberInfoRs>, MemberInfoRs>();
        CreateMap<UpdateInventory, UpdateInventoryRequest>()
            .ForMember(dest => dest.DateUpdated, mapper => mapper.MapFrom(src => src.ReceivedDateTime))
            .ForMember(dest => dest.ItemNumber, mapper => mapper.MapFrom(src => ConnectionStringNames.ItemNumber))
            .ForMember(dest => dest.RequestId, mapper => mapper.MapFrom(src => src.CorrelationId))
            .ForMember(dest => dest.Quantity, mapper => mapper.MapFrom(src => 1));

        CreateMap<InventoryUpdated, InventoryUpdateReceived>();
        CreateMap<CKD.Messages.Events.Result, CKD.Sagas.Result>();

        CreateMap<PdfDeliveredToClient, CreateOrUpdatePDFToClient>()
            .ForMember(dest => dest.DeliveryCreatedDateTime, mapper => mapper.MapFrom(src => src.CreatedDateTime));
        CreateMap<CreateOrUpdatePDFToClient, PDFToClient>();
        CreateMap<RCMBillingRequest, RCMBilling>();
        CreateMap<Data.Entities.CKD, RCMBillingRequest>()
            .ForMember(dest => dest.UsStateOfService, mapper => mapper.MapFrom(src => src.State))
            .ForMember(dest => dest.ClientId, mapper => mapper.MapFrom(src => src.ClientId))
            .ForMember(dest => dest.SharedClientId, mapper => mapper.MapFrom(src => src.ClientId))
            .ForMember(dest => dest.DateOfService, mapper => mapper.MapFrom(src => src.DateOfService));

        CreateMap<Data.Entities.CKD, ExamNotPerformed>()
            .ForMember(dest => dest.CreatedDateTime, mapper => mapper.MapFrom(src => DateTime.Now));

        CreateMap<CKDPerformed, Performed>()
            .ForMember(dest => dest.CreatedDate, mapper => mapper.MapFrom(src => src.CreatedDateTime))
            .ForMember(dest => dest.ReceivedDate, mapper => mapper.MapFrom(src => new DateTimeOffset(src.ReceivedDateTime, TimeSpan.Zero)));

        CreateMap<AddExamNotPerformed, NotPerformed>()
            .ForMember(dest => dest.CreatedDate, mapper => mapper.MapFrom(src => src.Exam.CreatedDateTime))
            .ForMember(dest => dest.ReceivedDate, mapper => mapper.MapFrom(src => new DateTimeOffset(src.Exam.ReceivedDateTime, TimeSpan.Zero)))
            .ForMember(dest => dest.MemberPlanId, mapper => mapper.MapFrom(src => src.Exam.MemberPlanId))
            .ForMember(dest => dest.ProviderId, mapper => mapper.MapFrom(src => src.Exam.ProviderId))
            .ForMember(dest => dest.EvaluationId, mapper => mapper.MapFrom(src => src.Exam.EvaluationId))
            .ForMember(dest => dest.ReasonNotes, mapper => mapper.MapFrom(src => src.Notes));

        CreateMap<Data.Entities.CKD, BillRequestSent>()
            .ForMember(dest => dest.CreatedDate, mapper => mapper.MapFrom(src => src.CreatedDateTime))
            .ForMember(dest => dest.ReceivedDate, mapper => mapper.MapFrom(src => new DateTimeOffset(src.ReceivedDateTime, TimeSpan.Zero)));

        CreateMap<Data.Entities.CKD, BillRequestNotSent>()
            .ForMember(dest => dest.CreatedDate, mapper => mapper.MapFrom(src => src.CreatedDateTime))
            .ForMember(dest => dest.ReceivedDate, mapper => mapper.MapFrom(src => new DateTimeOffset(src.ReceivedDateTime, TimeSpan.Zero)));

        CreateMap<Data.Entities.CKD, Result>()
            .ConvertUsing<ResultsMapper>();

        CreateMap<LookupCKDAnswer, Result>()
            .ConvertUsing<ResultsMapper>();

        CreateMap<Data.Entities.CKD, ProviderPayRequest>()
            .ForMember(dest => dest.ProviderProductCode, mapper => mapper.MapFrom(value => Application.ProductCode))
            .ForMember(dest => dest.PersonId, mapper => mapper.MapFrom(src => src.CenseoId))
            .ForMember(dest => dest.DateOfService, mapper => mapper.MapFrom(src => DateOnly.FromDateTime((DateTime)src.DateOfService).ToString("o")));

        CreateMap<ProviderPayRequest, ProviderPayApiRequest>();

        CreateMap<Data.Entities.CKD, ProviderPayRequestSent>()
            .ForMember(dest => dest.ProviderPayProductCode, mapper => mapper.MapFrom(value => Application.ProductCode))
            .ForMember(dest => dest.CreatedDate, mapper => mapper.MapFrom(src => src.CreatedDateTime.UtcDateTime))
            .ForMember(dest => dest.ReceivedDate, mapper => mapper.MapFrom(src => new DateTimeOffset(src.ReceivedDateTime, TimeSpan.Zero)))
            .ForMember(dest => dest.ProductCode, mapper => mapper.MapFrom(value => Application.ProductCode));

        CreateMap<Data.Entities.CKD, ProviderPayableEventReceived>()
            .ForMember(dest => dest.ReceivedDate, mapper => mapper.MapFrom(src => new DateTimeOffset(src.ReceivedDateTime, TimeSpan.Zero)))
            .ForMember(dest => dest.CreatedDate, mapper => mapper.MapFrom(src => src.CreatedDateTime.UtcDateTime))
            .ForMember(dest => dest.ProductCode, mapper => mapper.MapFrom(src => Application.ProductCode));

        CreateMap<Data.Entities.CKD, ProviderNonPayableEventReceived>()
            .ForMember(dest => dest.ReceivedDate, mapper => mapper.MapFrom(src => new DateTimeOffset(src.ReceivedDateTime, TimeSpan.Zero)))
            .ForMember(dest => dest.CreatedDate, mapper => mapper.MapFrom(src => src.CreatedDateTime.UtcDateTime))
            .ForMember(dest => dest.ProductCode, mapper => mapper.MapFrom(src => Application.ProductCode));
    }
}