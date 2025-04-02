using System;
using AutoMapper;
using Refit;
using Signify.A1C.Core.Events;
using Signify.A1C.Messages.Events;
using Signify.A1C.Svc.Core.ApiClient.Requests;
using Signify.A1C.Svc.Core.ApiClient.Response;
using Signify.A1C.Svc.Core.Commands;
using Signify.A1C.Svc.Core.Events;
using Signify.A1C.Svc.Core.Queries;
using Signify.A1C.Svc.Core.Sagas;
using A1CPerformedEvent = Signify.A1C.Messages.Events.A1CPerformedEvent;

namespace Signify.A1C.Svc.Core.Maps
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<EvaluationFinalizedEvent, A1CEvaluationReceived>();
            CreateMap<A1CEvaluationReceived, CreateOrUpdateA1C>()
                .ForMember(dest => dest.OrderCorrelationId, mapper => mapper.MapFrom(src => Guid.NewGuid()));
            CreateMap<MemberInfoRs, CreateOrUpdateA1C>()
                .ForMember(dest => dest.FirstName, mapper => mapper.MapFrom(src => src.FirstName))
                .ForMember(dest => dest.MiddleName, mapper => mapper.MapFrom(src => src.MiddleName))
                .ForMember(dest => dest.LastName, mapper => mapper.MapFrom(src => src.LastName))
                .ForMember(dest => dest.DateOfBirth, mapper => mapper.MapFrom(src => src.DateOfBirth))
                .ForMember(dest => dest.AddressLineOne, mapper => mapper.MapFrom(src => src.AddressLineOne))
                .ForMember(dest => dest.AddressLineTwo, mapper => mapper.MapFrom(src => src.AddressLineTwo))
                .ForMember(dest => dest.City, mapper => mapper.MapFrom(src => src.City))
                .ForMember(dest => dest.State, mapper => mapper.MapFrom(src => src.State))
                .ForMember(dest => dest.ZipCode, mapper => mapper.MapFrom(src => src.ZipCode));
            CreateMap<ProviderInfoRs, CreateOrUpdateA1C>()
                .ForMember(dest => dest.NationalProviderIdentifier,
                    mapper => mapper.MapFrom(src => src.NationalProviderIdentifier))
                .ForAllOtherMembers(src => src.Ignore());

            CreateMap<CreateOrUpdateA1C, Data.Entities.A1C>();
            CreateMap<Data.Entities.A1C, A1CPerformedEvent>()
                .ForMember(dest => dest.CorrelationId, mapper => mapper.MapFrom(src => Guid.NewGuid()));
            CreateMap<UpdateInventoryRequest, Data.Entities.A1C>();
            CreateMap<A1CEvaluationReceived, GetMemberInfo>();
            CreateMap<A1CEvaluationReceived, GetProviderInfo>();

            CreateMap<ApiResponse<MemberInfoRs>, MemberInfoRs>();
            CreateMap<ApiResponse<ProviderInfoRs>, MemberInfoRs>();

            CreateMap<A1CPerformedEvent, UpdateInventoryRequest>()
                .ForMember(dest => dest.DateUpdated, mapper => mapper.MapFrom(src => src.ReceivedDateTime))
                .ForMember(dest => dest.SerialNumber,
                    mapper => mapper.MapFrom(src => string.IsNullOrEmpty(src.Barcode) ? null : src.Barcode))
                .ForMember(dest => dest.ItemNumber, mapper => mapper.MapFrom(src => Constants.ItemNumber))
                .ForMember(dest => dest.RequestId, mapper => mapper.MapFrom(src => src.CorrelationId));
            CreateMap<InventoryUpdated, InventoryUpdateReceived>();

            CreateMap<BarcodeUpdate, UpdateBarcodeHistory>();
            CreateMap<Data.Entities.A1C, CreateOrUpdateA1C>();
            CreateMap<UpdateBarcodeHistory, Data.Entities.A1CBarcodeHistory>();
            CreateMap<Events.Result, Sagas.Result>();
            CreateMap<EvaluationModel, A1CEvaluationReceived>()
                .ForMember(dest => dest.Id, mapper => mapper.MapFrom(src => Guid.NewGuid()))
                .ForMember(dest => dest.ApplicationId, mapper => mapper.MapFrom(src => "Signify.Labs.Api"));
            CreateMap<BarcodeUpdate, A1CEvaluationReceived>();

            CreateMap<HomeAccessResultsReceived, CreateLabResult>();
		    CreateMap<CreateLabResult, Data.Entities.LabResults>()
                .ForMember(dest => dest.ReceivedDateTime, mapper => mapper.MapFrom( src => DateTime.UtcNow))
                .ForMember(dest => dest.ProductCode, mapper => mapper.MapFrom(src => src.LabTestType))
                .ForMember(dest => dest.LabResult, mapper => mapper.MapFrom(src => src.LabResults));

            CreateMap<Data.Entities.A1C, UpdateInventoryRequest>()
                .ForMember(dest => dest.DateUpdated, mapper => mapper.MapFrom(src => src.ReceivedDateTime))
                .ForMember(dest => dest.SerialNumber, mapper => mapper.MapFrom(src => string.IsNullOrEmpty(src.Barcode) ? null : src.Barcode))
                .ForMember(dest => dest.EvaluationId, mapper => mapper.MapFrom(src => src.EvaluationId))
                .ForMember(dest => dest.ProviderId, mapper => mapper.MapFrom(src => src.ProviderId))
                .ForMember(dest => dest.A1CId, mapper => mapper.MapFrom(src => src.A1CId))
                .ForMember(dest => dest.ItemNumber, mapper => mapper.MapFrom(src => Constants.ItemNumber));

            MapA1COrderEvent();
            MapOrderEventToRequest();
        }

        private void MapA1COrderEvent()
        {
            CreateMap<Data.Entities.A1C, CreateOrderEvent>()
                    .ForMember(dest => dest.FirstName, mapper => mapper.MapFrom(src => src.FirstName))
                    .ForMember(dest => dest.LastName, mapper => mapper.MapFrom(src => src.LastName))
                    .ForMember(dest => dest.DateOfBirth, mapper => mapper.MapFrom(src => src.DateOfBirth))
                    .ForMember(dest => dest.AddressLineOne, mapper => mapper.MapFrom(src => src.AddressLineOne))
                    .ForMember(dest => dest.AddressLineTwo, mapper => mapper.MapFrom(src => src.AddressLineTwo))
                    .ForMember(dest => dest.City, mapper => mapper.MapFrom(src => src.City))
                    .ForMember(dest => dest.State, mapper => mapper.MapFrom(src => src.State))
                    .ForMember(dest => dest.ZipCode, mapper => mapper.MapFrom(src => src.ZipCode))
                    .ForMember(dest => dest.EvaluationId, mapper => mapper.MapFrom(src => src.EvaluationId))
                    .ForMember(dest => dest.MemberPlanId, mapper => mapper.MapFrom(src => src.MemberPlanId))
                    .ForMember(dest => dest.CenseoId, mapper => mapper.MapFrom(src => src.CenseoId))
                    .ForMember(dest => dest.DateOfService, mapper => mapper.MapFrom(src => src.DateOfService))
                    .ForMember(dest => dest.AppointmentId, mapper => mapper.MapFrom(src => src.AppointmentId))
                    .ForMember(dest => dest.Barcode, mapper => mapper.MapFrom(src => src.Barcode))
                    .ForMember(dest => dest.ClientId, mapper => mapper.MapFrom(src => src.ClientId))
                    .ForMember(dest => dest.CorrelationId, mapper => mapper.MapFrom(src => src.OrderCorrelationId))
                    .ForAllOtherMembers(src => src.Ignore())
                    ;
        }

        private void MapOrderEventToRequest()
        {
            CreateMap<CreateOrderEvent, CreateOrder>()
                    .ForMember(dest => dest.FirstName, mapper => mapper.MapFrom(src => src.FirstName))
                    .ForMember(dest => dest.LastName, mapper => mapper.MapFrom(src => src.LastName))
                    .ForMember(dest => dest.DOB, mapper => mapper.MapFrom(src => src.DateOfBirth))
                    .ForMember(dest => dest.Address1, mapper => mapper.MapFrom(src => src.AddressLineOne))
                    .ForMember(dest => dest.Address2, mapper => mapper.MapFrom(src => src.AddressLineTwo))
                    .ForMember(dest => dest.City, mapper => mapper.MapFrom(src => src.City))
                    .ForMember(dest => dest.State, mapper => mapper.MapFrom(src => src.State))
                    .ForMember(dest => dest.ZipCode, mapper => mapper.MapFrom(src => src.ZipCode))
                    .ForMember(dest => dest.EvaluationId, mapper => mapper.MapFrom(src => int.Parse(src.EvaluationId)))
                    .ForMember(dest => dest.MemberPlanId, mapper => mapper.MapFrom(src => src.MemberPlanId))
                    .ForMember(dest => dest.PlanId, mapper => mapper.MapFrom(src => int.Parse(src.PlanId)))
                    .ForMember(dest => dest.CenseoId, mapper => mapper.MapFrom(src => src.CenseoId))
                    .ForMember(dest => dest.DateOfService, mapper => mapper.MapFrom(src => src.DateOfService))
                    .ForMember(dest => dest.SampleId, mapper => mapper.MapFrom(src => src.Barcode))
                    .ForMember(dest => dest.ClientId, mapper => mapper.MapFrom(src => src.ClientId.ToString()))
                    .ForMember(dest => dest.AppointmentId, mapper => mapper.MapFrom(src => src.AppointmentId.ToString()))
                    .ForMember(dest => dest.OrderCorrelationId, mapper => mapper.MapFrom(src => src.CorrelationId))
                    .ForMember(dest => dest.SampleType, mapper => mapper.MapFrom(src => src.LabTestType))
                    .ForMember(dest => dest.HomePhone, mapper => mapper.MapFrom(src => src.HomePhone))
                    .ForMember(dest => dest.Gender, mapper => mapper.MapFrom(src => src.Sex))
                    .ForMember(dest => dest.ProviderName, mapper => mapper.MapFrom(src => src.ProviderName))
                    .ForMember(dest => dest.SubscriberId, mapper => mapper.MapFrom(src => src.SubscriberId))
                    .ForAllOtherMembers(src => src.Ignore())
                    ;
        }
    }
}