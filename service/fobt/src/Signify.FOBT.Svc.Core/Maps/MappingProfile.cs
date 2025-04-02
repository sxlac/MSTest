using AutoMapper;
using FobtNsbEvents;
using Refit;
using Signify.FOBT.Messages.Events;
using Signify.FOBT.Messages.Events.Status;
using Signify.FOBT.Svc.Core.ApiClient.Requests;
using Signify.FOBT.Svc.Core.ApiClient.Response;
using Signify.FOBT.Svc.Core.Commands;
using Signify.FOBT.Svc.Core.Constants;
using Signify.FOBT.Svc.Core.Data.Entities;
using Signify.FOBT.Svc.Core.Events;
using Signify.FOBT.Svc.Core.Events.Status;
using Signify.FOBT.Svc.Core.Models;
using Signify.FOBT.Svc.Core.Queries;
using Signify.FOBT.Svc.Core.Sagas;
using System;
using Fobt = Signify.FOBT.Svc.Core.Data.Entities.FOBT;
using FOBTPerformedEvent = Signify.FOBT.Messages.Events.FOBTPerformedEvent;
using OrderHeldEvent = Signify.FOBT.Svc.Core.Events.OrderHeld;
using OrderHeldStatus = Signify.FOBT.Svc.Core.Events.Status.OrderHeld;
using Context = Signify.FOBT.Svc.Core.Events.Status.OrderHeldContext;

namespace Signify.FOBT.Svc.Core.Maps
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            MapDateTimes();
            
            CreateMap<EvaluationFinalizedEvent, FobtEvalReceived>();
            CreateMap<FobtEvalReceived, EvaluationReceived>();
            CreateMap<CreateLabResult, LabResults>();
            CreateMap<CreateLabResult, HomeAccessResultsReceived>().ForMember(dest => dest.LabTestType, mapper => mapper.MapFrom(src => src.ProductCode));
            CreateMap<HomeAccessResultsReceived, CreateLabResult>().ForMember(dest => dest.ProductCode, mapper => mapper.MapFrom(src => src.LabTestType))
                .ForMember(dest => dest.LabResult, mapper => mapper.MapFrom(src => src.LabResults));
            CreateMap<RCMRequestEvent, RCMBilling>();
            CreateMap<Fobt, RCMRequestEvent>()
                .ForMember(dest => dest.UsStateOfService, mapper => mapper.MapFrom(src => src.State))
                .ForMember(dest => dest.SharedClientId, mapper => mapper.MapFrom(src => src.ClientId == null ? 0 : src.ClientId));
            CreateMap<EvaluationReceived, CreateOrUpdateFOBT>()
                .ForMember(dest => dest.OrderCorrelationId, mapper => mapper.MapFrom(src => Guid.NewGuid()));
            CreateMap<MemberInfoRs, CreateOrUpdateFOBT>()
                .ForMember(dest => dest.FirstName, mapper => mapper.MapFrom(src => src.FirstName))
                .ForMember(dest => dest.MiddleName, mapper => mapper.MapFrom(src => src.MiddleName))
                .ForMember(dest => dest.LastName, mapper => mapper.MapFrom(src => src.LastName))
                .ForMember(dest => dest.DateOfBirth, mapper => mapper.MapFrom(src => src.DateOfBirth))
                .ForMember(dest => dest.AddressLineOne, mapper => mapper.MapFrom(src => src.AddressLineOne))
                .ForMember(dest => dest.AddressLineTwo, mapper => mapper.MapFrom(src => src.AddressLineTwo))
                .ForMember(dest => dest.City, mapper => mapper.MapFrom(src => src.City))
                .ForMember(dest => dest.State, mapper => mapper.MapFrom(src => src.State))
                .ForMember(dest => dest.ZipCode, mapper => mapper.MapFrom(src => src.ZipCode));
            CreateMap<ProviderInfoRs, CreateOrUpdateFOBT>()
                .ForMember(dest => dest.NationalProviderIdentifier, mapper => mapper.MapFrom(src => src.NationalProviderIdentifier))
                .ForMember(dest => dest.FirstName, mapper => mapper.Ignore()) // Do not overwrite Exam.FirstName from the provider's first name
                .ForMember(dest => dest.LastName, mapper => mapper.Ignore()); // Do not overwrite Exam.LastName from the provider's last name
            CreateMap<Fobt, CreateOrUpdateFOBT>();

            CreateMap<CreateOrUpdateFOBT, Fobt>().ReverseMap();
            CreateMap<Fobt, FOBTPerformedEvent>()
                .ForMember(dest => dest.CorrelationId, mapper => mapper.MapFrom(src => Guid.NewGuid()));
            CreateMap<UpdateInventoryRequest, Fobt>();
            CreateMap<EvaluationReceived, GetMemberInfo>();
            CreateMap<EvaluationReceived, GetProviderInfo>();

            CreateMap<BarcodeUpdate, EvaluationReceived>()
                .ForMember(dest => dest.ApplicationId, mapper => mapper.MapFrom(src => "Signify.Labs.Api"));

            CreateMap<ApiResponse<MemberInfoRs>, MemberInfoRs>();
            CreateMap<ApiResponse<ProviderInfoRs>, MemberInfoRs>();

            CreateMap<FOBTPerformedEvent, UpdateInventoryRequest>()
                .ForMember(dest => dest.DateUpdated, mapper => mapper.MapFrom(src => src.ReceivedDateTime))
                .ForMember(dest => dest.SerialNumber,
                    mapper => mapper.MapFrom(src => string.IsNullOrEmpty(src.Barcode) ? null : src.Barcode))
                .ForMember(dest => dest.ItemNumber, mapper => mapper.MapFrom(src => ApplicationConstants.ItemNumber))
                .ForMember(dest => dest.RequestId, mapper => mapper.MapFrom(src => src.CorrelationId));
            CreateMap<InventoryUpdated, InvUpdateReceived>();
            CreateMap<Events.Result, Sagas.Result>();

            CreateMap<Fobt, UpdateInventoryRequest>()
                .ForMember(dest => dest.DateUpdated, mapper => mapper.MapFrom(src => src.ReceivedDateTime))
                .ForMember(dest => dest.SerialNumber, mapper => mapper.MapFrom(src => string.IsNullOrEmpty(src.Barcode) ? null : src.Barcode))
                .ForMember(dest => dest.EvaluationId, mapper => mapper.MapFrom(src => src.EvaluationId))
                .ForMember(dest => dest.ProviderId, mapper => mapper.MapFrom(src => src.ProviderId))
                .ForMember(dest => dest.FOBTId, mapper => mapper.MapFrom(src => src.FOBTId))
                .ForMember(dest => dest.ItemNumber, mapper => mapper.MapFrom(src => ApplicationConstants.ItemNumber));

            MapCreateOrderEvent();
            MapOrderEventToRequest();

            CreateMap<PdfDeliveredToClient, CreateOrUpdatePDFToClient>()
                .ForMember(dest => dest.DeliveryCreatedDateTime, mapper => mapper.MapFrom(src => src.CreatedDateTime));
            CreateMap<CreateOrUpdatePDFToClient, PDFToClient>();

            CreateMap<Fobt, Performed>()
                .ForMember(dest => dest.CreatedDate, mapper => mapper.MapFrom(src => src.CreatedDateTime))
                .ForMember(dest => dest.ReceivedDate, mapper => mapper.MapFrom(src => src.ReceivedDateTime));
            CreateMap<Fobt, NotPerformed>()
                .ForMember(dest => dest.CreatedDate, mapper => mapper.MapFrom(src => src.CreatedDateTime))
                .ForMember(dest => dest.ReceivedDate, mapper => mapper.MapFrom(src => src.ReceivedDateTime));
            CreateMap<Fobt, BillRequestSent>()
                .ForMember(dest => dest.CreatedDate, mapper => mapper.MapFrom(src => src.CreatedDateTime))
                .ForMember(dest => dest.ReceivedDate, mapper => mapper.MapFrom(src => src.ReceivedDateTime));
            CreateMap<Fobt, BillRequestNotSent>()
                .ForMember(dest => dest.CreatedDate, mapper => mapper.MapFrom(src => src.CreatedDateTime))
                .ForMember(dest => dest.BillingProductCode, mapper => mapper.MapFrom(src => ApplicationConstants.BILLING_PRODUCT_CODE_RESULTS));

            CreateMap<Fobt, OrderHeldStatus>()
                .ForMember(dest => dest.CreatedDate, mapper => mapper.MapFrom(src => src.CreatedDateTime))
                .ForMember(dest => dest.ReceivedDate, mapper => mapper.MapFrom(src => src.ReceivedDateTime))
                .ForMember(dest => dest.Context, mapper => mapper.MapFrom(src => new Context { OrderId = src.OrderCorrelationId, Barcode = src.Barcode}));

            CreateMap<OrderHeldEvent, OrderHeldStatus>()
                .ForMember(dest => dest.HoldCreatedDate, mapper => mapper.MapFrom(src => src.CreatedDateTime))
                .AfterMap((src, dest) =>
                {
                    dest.Context ??= new Context();
                    dest.Context.SampleReceivedDate = src.LabReceivedDate;
                });

            CreateMap<NotPerformedReason, FOBTNotPerformed>()
                .ForMember(dest => dest.NotPerformedReasonId, mapper => mapper.MapFrom(src => src.NotPerformedReasonId));

            CreateMap<Fobt, Results>()
                .ConvertUsing<ResultsReceivedMapper>();
            CreateMap<LabResults, Results>()
                .ConvertUsing<ResultsReceivedMapper>();

            CreateMap<NotPerformedReasonResult, NotPerformed>();

            CreateMap<Fobt, ProviderPayRequestSent>()
                .ForMember(dest => dest.ProviderPayProductCode,
                    mapper => mapper.MapFrom(value => ApplicationConstants.PRODUCT_CODE));
            CreateMap<ProviderPayRequest, ProviderPayApiRequest>()
                .ForMember(dest => dest.DateOfService, mapper => mapper.MapFrom(src => src.DateOfService.ToString()));
            CreateMap<Fobt, ProviderPayableEventReceived>();

            CreateMap<Fobt, ProviderNonPayableEventReceived>();
            
            CreateMap<Fobt, ProviderPayRequest>()
                .ForMember(dest => dest.ProviderProductCode, mapper => mapper.MapFrom(value => ApplicationConstants.PRODUCT_CODE))
                .ForMember(dest => dest.DateOfService, mapper => mapper.MapFrom(src => DateOnly.FromDateTime(src.DateOfService.Value).ToString("o")))
                .ForMember(dest => dest.PersonId, mapper => mapper.MapFrom(src => src.CenseoId))
                .ForMember(dest => dest.ExamId, mapper => mapper.MapFrom(src => src.FOBTId));
        }

        private void MapDateTimes()
        {
            CreateMap<DateTime, DateTime>().ConvertUsing((s, d) => DateTime.SpecifyKind(s, DateTimeKind.Utc));
            CreateMap<DateTimeOffset, DateTimeOffset>().ConvertUsing((s, d) => s.ToUniversalTime());
            CreateMap<DateTimeOffset?, DateTimeOffset?>().ConvertUsing((s, d) => (s.HasValue) ? s.Value.ToUniversalTime() : null);
            CreateMap<DateTime?, DateTime?>().ConvertUsing((s, d) =>
            {
                if (s.HasValue)
                {
                    return DateTime.SpecifyKind(s.Value, DateTimeKind.Utc);
                }
                return null;
            });
        }

        private void MapCreateOrderEvent()
        {
            CreateMap<Fobt, CreateOrderEvent>()
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
                .ForMember(dest => dest.CorrelationId, mapper => mapper.MapFrom(src => src.OrderCorrelationId));
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
                .ForMember(dest => dest.DateOfService, mapper => mapper.MapFrom(src => DateTime.SpecifyKind(src.DateOfService.Value, DateTimeKind.Utc)))
                .ForMember(dest => dest.SampleId, mapper => mapper.MapFrom(src => src.Barcode))
                .ForMember(dest => dest.ClientId, mapper => mapper.MapFrom(src => src.ClientId == default ? "" : src.ClientId.ToString()))
                .ForMember(dest => dest.AppointmentId, mapper => mapper.MapFrom(src => src.AppointmentId == default ? "" : src.AppointmentId.ToString()))
                .ForMember(dest => dest.OrderCorrelationId, mapper => mapper.MapFrom(src => src.CorrelationId))
                .ForMember(dest => dest.SampleType, mapper => mapper.MapFrom(src => src.LabTestType))
                .ForMember(dest => dest.HomePhone, mapper => mapper.MapFrom(src => src.HomePhone))
                .ForMember(dest => dest.Gender, mapper => mapper.MapFrom(src => src.Sex))
                .ForMember(dest => dest.ProviderName, mapper => mapper.MapFrom(src => src.ProviderName))
                .ForMember(dest => dest.SubscriberId, mapper => mapper.MapFrom(src => src.SubscriberId));
        }
    }
}