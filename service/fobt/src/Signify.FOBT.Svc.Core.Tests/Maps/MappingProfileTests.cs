using AutoMapper;
using FluentAssertions;
using OrderHeld = Signify.FOBT.Svc.Core.Events.OrderHeld;
using OrderHeldStatus = Signify.FOBT.Svc.Core.Events.Status.OrderHeld;
using Signify.FOBT.Messages.Events;
using Signify.FOBT.Svc.Core.ApiClient.Requests;
using Signify.FOBT.Svc.Core.ApiClient.Response;
using Signify.FOBT.Svc.Core.Commands;
using Signify.FOBT.Svc.Core.Constants;
using Signify.FOBT.Svc.Core.Data.Entities;
using Signify.FOBT.Svc.Core.Events;
using Signify.FOBT.Svc.Core.Maps;
using Signify.FOBT.Svc.Core.Tests.Mocks.Models;
using System.Collections.Generic;
using System;
using Xunit;

namespace Signify.FOBT.Svc.Core.Tests.Maps;

public class MappingProfileTests
{
    private static IMapper CreateSubject()
    {
        return new MapperConfiguration(configure =>
        {
            configure.AddProfile<MappingProfile>();
        }).CreateMapper();
    }

    [Fact]
    public void Should_Map_FOBTPerformed_From_FOBT()
    {
        var fobt = new Core.Data.Entities.FOBT();

        var mapper = CreateSubject();

        //Action
        var ckdPerformed = mapper.Map<Core.Data.Entities.FOBT, FOBTPerformedEvent>(fobt);

        //Assert
        ckdPerformed.CorrelationId.Should().NotBeEmpty();
    }

        
    [Fact]
    public void Should_Map_OrderHeldStatus_From_FOBT()
    {
        var fobt = new Core.Data.Entities.FOBT
        {
            FOBTId = 1,
            EvaluationId = 6,
            MemberPlanId = 65743,
            ProviderId = 10,
            ReceivedDateTime = DateTime.UtcNow,
            CreatedDateTime = DateTime.UtcNow,
            OrderCorrelationId = Guid.NewGuid(),
            Barcode = "somebarcode"
        };

        var orderHeldEvent = new OrderHeld
        {
            Barcode = "somebarcode",
            HoldReasons = new List<string> { "Collection Date Missing" },
            LabReceivedDate = DateTime.UtcNow,
            CreatedDateTime = DateTime.UtcNow
        };

        var mapper = CreateSubject();

        //Action
        var orderHeldStatus = mapper.Map<Core.Data.Entities.FOBT, OrderHeldStatus>(fobt);

        //Assert
        orderHeldStatus.EvaluationId.Should().Be(fobt.EvaluationId);
        orderHeldStatus.MemberPlanId.Should().Be(fobt.MemberPlanId);
        orderHeldStatus.ProviderId.Should().Be(fobt.ProviderId);
        orderHeldStatus.ReceivedDate.Should().BeSameDateAs(fobt.ReceivedDateTime);
        orderHeldStatus.CreatedDate.Should().BeSameDateAs(fobt.CreatedDateTime);
        orderHeldStatus.ProductCode.Should().Be(ApplicationConstants.PRODUCT_CODE);
        orderHeldStatus.Context.OrderId.Should().Be(fobt.OrderCorrelationId);
        orderHeldStatus.Context.Barcode.Should().Be(fobt.Barcode);
    }

    [Fact]
    public void Should_Map_OrderHeldStatus_From_OrderHeldEvent()
    {
        var orderHeldEvent = new OrderHeld
        {
            HoldReasons = new List<string> { "Collection Date Missing" },
            LabReceivedDate = DateTime.UtcNow,
            CreatedDateTime = DateTime.UtcNow
        };

        var mapper = CreateSubject();

        //Action
        var orderHeldStatus = mapper.Map<OrderHeld, OrderHeldStatus>(orderHeldEvent);

        //Assert
        Assert.Equal(orderHeldStatus.HoldReasons, orderHeldEvent.HoldReasons);
        orderHeldStatus.Context.SampleReceivedDate.Should().Be(orderHeldEvent.LabReceivedDate);
        orderHeldStatus.HoldCreatedDate.Should().BeSameDateAs(orderHeldEvent.CreatedDateTime);
    }

    [Fact]
    public void Fobt_To_ProviderPayRequest()
    {
        var mapper = CreateSubject();
        DateTime? dos = new DateTime(2023, 1, 21, 16, 23, 42, DateTimeKind.Utc);
            
        const string dateString = "2023-01-21";
        const string censeoId = "567";
        const int evaluationId = 12;
        var source = new Core.Data.Entities.FOBT
        {
            FOBTId = 1, EvaluationId = evaluationId, MemberPlanId = 65743, ProviderId = 10, CreatedDateTime = DateTimeOffset.UtcNow.DateTime, DateOfService = dos,
            ClientId = 15, CenseoId = censeoId,
        };
        var actual = mapper.Map<ProviderPayRequest>(source);
        actual.ProviderProductCode.Should().Be(ApplicationConstants.PRODUCT_CODE);
        actual.ProviderId.Should().Be(10);
        actual.EvaluationId.Should().Be(evaluationId);
        actual.PersonId.Should().Be(censeoId);
        actual.DateOfService.Should().Be(dateString);
    }

    [Fact]
    public void Map_FromProviderInfoRs_ToCreateOrUpdateFobt_Test()
    {
        // Arrange
        var source = new ProviderInfoRs
        {
            FirstName = "ProviderFirstName",
            LastName = "ProviderLastName",
            NationalProviderIdentifier = "NPI"
        };

        var destination = new CreateOrUpdateFOBT
        {
            FirstName = "MemberFirstName",
            LastName = "MemberLastName"
        };

        var subject = CreateSubject();

        // Act
        var actual = subject.Map(source, destination);

        // Assert
        Assert.Equal("MemberFirstName", actual.FirstName);
        Assert.Equal("MemberFirstName", destination.FirstName);
        Assert.Equal("MemberLastName", actual.LastName);
        Assert.Equal("MemberLastName", destination.LastName);

        Assert.Equal("NPI", actual.NationalProviderIdentifier);
        Assert.Equal("NPI", destination.NationalProviderIdentifier);
    }
    [Fact]
    public void Map_CreateOrUpdatePDFToClientToPDFToClient_MapDateTimeKindCorrectly()
    {
        // Arrange
        var inputDate = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local);
        var subject = CreateSubject();
        var createOrUpdatePdfToClient = new CreateOrUpdatePDFToClient
        {
            BatchId = 101219,
            BatchName = $"uploadbatch_101219_Evaluation_{DateTime.Now.Month}_{DateTime.Now.Day}_{DateTime.Now.Year}",
            DeliveryDateTime = inputDate,
            DeliveryCreatedDateTime = inputDate,
            EvaluationId = 10000000,
            EventId = Guid.NewGuid(),
            FOBTId = 10000
        };

        // Act
        var actual = subject.Map<PDFToClient>(createOrUpdatePdfToClient);

        // Assert
        Assert.Equal(DateTimeKind.Utc, actual.DeliveryCreatedDateTime.Kind);
        Assert.Equal(DateTimeKind.Utc, actual.DeliveryDateTime.Kind);
    }
    [Fact]
    public void Map_FromRCMRequestEventToRCMBilling_Test()
    {
        // Arrange
        var source = new RCMRequestEvent
        {
            FOBT = FobtEntityMock.BuildFobt(1),
            FOBTId = 1,
            SharedClientId = 55,
            BillingProductCode = "1",
            AdditionalDetails =  new Dictionary<string, string>
            {
                { "BatchName", "Luke" },
                { "EvaluationId", "1234"},
                { "appointmentId", "5678" }
            }
        };

        var destination = new RCMBilling();

        var subject = CreateSubject();

        // Act
        var actual = subject.Map(source, destination);

        // Assert we're mapping AdditionalDetails
        Assert.Equal("5678", actual.AdditionalDetails["appointmentId"]);
        Assert.Equal("1234", actual.AdditionalDetails["EvaluationId"]);
        Assert.Equal("Luke", actual.AdditionalDetails["BatchName"]);
            
        Assert.Equal(55, actual.SharedClientId);
            
    }
}