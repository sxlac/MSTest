using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using Signify.HBA1CPOC.Messages.Events.Akka;
using Signify.HBA1CPOC.Messages.Events.Status;
using Signify.HBA1CPOC.Messages.Events;
using Signify.HBA1CPOC.Sagas;
using Signify.HBA1CPOC.Svc.Core.ApiClient.Requests;
using Signify.HBA1CPOC.Svc.Core.ApiClient.Response;
using Signify.HBA1CPOC.Svc.Core.Commands;
using Signify.HBA1CPOC.Svc.Core.Data.Entities;
using Signify.HBA1CPOC.Svc.Core.DI.Configs;
using Signify.HBA1CPOC.Svc.Core.Models;
using Signify.HBA1CPOC.Svc.Core.Sagas.Commands;
using System.Net.Http;
using System;
using Xunit;
using Result = Signify.HBA1CPOC.Messages.Events.Result;

namespace Signify.HBA1CPOC.Svc.Core.Tests.Maps;

public class MappingProfileTests
{
    private static IMapper CreateSubject()
    {
        var services = new ServiceCollection().AddSingleton<MappingProfileTests>();
        return AutoMapperConfig.AddAutoMapper(services);
    }

    [Fact]
    public void Should_Map_CKDPerformed_From_HBA1CPOC()
    {
        var hba1Cpoc = new Core.Data.Entities.HBA1CPOC();

        var mapper = CreateSubject();

        //Action
        var ckdPerformed = mapper.Map<Core.Data.Entities.HBA1CPOC, A1CPOCPerformed>(hba1Cpoc);

        //Assert
        ckdPerformed.CorrelationId.Should().NotBeEmpty();
    }

    [Fact]
    public void Should_Map_UpdateInventory()
    {
        var updateInv = new UpdateInventory();

        var mapper = CreateSubject();

        //Action
        var req = mapper.Map<UpdateInventory, UpdateInventoryRequest>(updateInv);

        //Assert
        req.RequestId.Should().Be(updateInv.CorrelationId);
        req.DateUpdated.Should().Be(updateInv.ReceivedDateTime);
    }

    [Fact]
    public void Should_Map_A1CPOCPerformed()
    {
        var a1CpocPerformed = new A1CPOCPerformed();

        var mapper = CreateSubject();

        //Action
        var req = mapper.Map<A1CPOCPerformed, Core.Data.Entities.HBA1CPOC>(a1CpocPerformed);

        //Assert
        req.MemberPlanId.Should().Be(a1CpocPerformed.MemberPlanId);
    }

    [Fact]
    public void Should_Map_Result()
    {
        var result = new Result();

        var mapper = CreateSubject();

        //Action
        var req = mapper.Map<Result, HBA1CPOC.Sagas.Result>(result);

        //Assert
        req.ErrorMessage.Should().Be(result.ErrorMessage);
    }

    [Fact]
    public void Should_Map_InventoryUpdated()
    {
        var now = DateTime.Now;
        var inventoryUpdated = new InventoryUpdated
        {
            RequestId = Guid.NewGuid(),
            ItemNumber = string.Empty,
            Result = new Result(),
            SerialNumber = string.Empty,
            Quantity = default,
            ProviderId = default,
            DateUpdated = now,
            ExpirationDate = now.AddDays(5)
        };

        var mapper = CreateSubject();

        //Action
        var req = mapper.Map<InventoryUpdated, InventoryUpdateReceived>(inventoryUpdated);

        //Assert
        req.DateUpdated.Should().Be(inventoryUpdated.DateUpdated);
        req.ExpirationDate.Should().Be(DateOnly.FromDateTime(now.AddDays(5)));
    }

    [Fact]
    public void Should_Map_MemberInfoRs()
    {
        var result = new ApiResponse<MemberInfoRs>(new HttpResponseMessage(), new MemberInfoRs(), null!);

        var mapper = CreateSubject();

        //Action
        var req = mapper.Map<ApiResponse<MemberInfoRs>, MemberInfoRs>(result);

        //Assert
        req.AddressLineOne.Should().Be(result.Content.AddressLineOne);
    }

    [Fact]
    public void Should_Map_MemberInfoRs_CreateOrUpdateHBA1CPoc()
    {
        var now = DateTime.UtcNow;
        var mir = new MemberInfoRs
        {
            DateOfBirth = now
        };
        var create = new CreateOrUpdateHBA1CPOC
        {
            DateOfBirth = DateOnly.FromDateTime(now)
        };
        var mapper = CreateSubject();

        //Action
        var req = mapper.Map<MemberInfoRs, CreateOrUpdateHBA1CPOC>(mir);

        //Assert
        req.DateOfBirth.Should().Be(create.DateOfBirth);
    }

    [Fact]
    public void Should_Map_EvaluationAnswers_CreateOrUpdateHBA1CPoc()
    {
        var now = DateTime.UtcNow;
        var evaluationAnswers = new EvaluationAnswers
        {
            ExpirationDate = now
        };
        var create = new CreateOrUpdateHBA1CPOC
        {
            ExpirationDate = DateOnly.FromDateTime(now)
        };
        var mapper = CreateSubject();

        //Action
        var req = mapper.Map<EvaluationAnswers, CreateOrUpdateHBA1CPOC>(evaluationAnswers);

        //Assert
        req.ExpirationDate.Should().Be(create.ExpirationDate);
    }

    [Fact]
    public void Should_Map_CreateOrUpdateHBA1CPoc_HBA1CPoc()
    {
        var now = DateTime.UtcNow;
        var create = new CreateOrUpdateHBA1CPOC
        {
            DateOfBirth = DateOnly.FromDateTime(now),
            ExpirationDate = DateOnly.FromDateTime(now)
        };
        var hba = new Core.Data.Entities.HBA1CPOC
        {
            DateOfBirth = DateOnly.FromDateTime(now),
            ExpirationDate = DateOnly.FromDateTime(now)
        };
        var mapper = CreateSubject();

        //Action
        var req = mapper.Map<CreateOrUpdateHBA1CPOC, Core.Data.Entities.HBA1CPOC>(create);

        //Assert
        req.DateOfBirth.Should().Be(hba.DateOfBirth);
        req.ExpirationDate.Should().Be(hba.ExpirationDate);
    }

    [Fact]
    public void Should_Map_HBA1CPoc_CreateOrUpdateHBA1CPoc()
    {
        var now = DateTime.UtcNow;
        var create = new CreateOrUpdateHBA1CPOC
        {
            DateOfBirth = DateOnly.FromDateTime(now),
            ExpirationDate = DateOnly.FromDateTime(now)
        };
        var hba = new Core.Data.Entities.HBA1CPOC
        {
            DateOfBirth = DateOnly.FromDateTime(now),
            ExpirationDate = DateOnly.FromDateTime(now)
        };
        var mapper = CreateSubject();

        //Action
        var req = mapper.Map<Core.Data.Entities.HBA1CPOC, CreateOrUpdateHBA1CPOC>(hba);

        //Assert
        req.DateOfBirth.Should().Be(create.DateOfBirth);
        req.ExpirationDate.Should().Be(create.ExpirationDate);
    }

    [Fact]
    public void Map_From_Hba1cPocEntity_To_Performed_Test()
    {
        var source = new Core.Data.Entities.HBA1CPOC
        {
            CreatedDateTime = DateTimeOffset.UtcNow,
            ReceivedDateTime = DateTime.UtcNow
        };

        var subject = CreateSubject();

        var actual = subject.Map<Performed>(source);

        Assert.Equal("HBA1CPOC", actual.ProductCode);
        Assert.Equal(source.CreatedDateTime, actual.CreateDate);
        Assert.Equal(source.ReceivedDateTime, actual.ReceivedDate);
    }

    [Fact]
    public void Map_From_Hba1cPocEntity_To_NotPerformed_Test()
    {
        var source = new Core.Data.Entities.HBA1CPOC
        {
            CreatedDateTime = DateTimeOffset.UtcNow,
            ReceivedDateTime = DateTime.UtcNow
        };

        var subject = CreateSubject();

        var actual = subject.Map<NotPerformed>(source);

        Assert.Equal("HBA1CPOC", actual.ProductCode);
        Assert.Equal(source.CreatedDateTime, actual.CreateDate);
        Assert.Equal(source.ReceivedDateTime, actual.ReceivedDate);
    }

    [Fact]
    public void Map_From_Hba1cPocStatus_To_BillRequestSent_Test()
    {
        var source = new HBA1CPOCStatus
        {
            CreatedDateTime = DateTimeOffset.UtcNow,
            HBA1CPOC = new Core.Data.Entities.HBA1CPOC
            {
                EvaluationId = 1,
                ProviderId = 2,
                MemberPlanId = 3,
                ReceivedDateTime = DateTime.UtcNow
            }
        };

        var subject = CreateSubject();

        var actual = subject.Map<BillRequestSent>(source);

        Assert.Equal("HBA1CPOC", actual.ProductCode);
        Assert.Equal("HBA1CPOC", actual.BillingProductCode);
        Assert.Equal(source.CreatedDateTime, actual.CreateDate);
        Assert.Equal(source.HBA1CPOC.ProviderId, actual.ProviderId);
        Assert.Equal(source.HBA1CPOC.EvaluationId, actual.EvaluationId);
        Assert.Equal(source.HBA1CPOC.MemberPlanId, (int)actual.MemberPlanId);
    }

    [Fact]
    public void Map_From_Hba1cPocStatus_And_HBA1CPOC_To_BillRequestSent_Test()
    {
        var source = new HBA1CPOCStatus
        {
            CreatedDateTime = DateTimeOffset.UtcNow,
        };
        var entity = new Core.Data.Entities.HBA1CPOC
        {
            EvaluationId = 1,
            ProviderId = 2,
            MemberPlanId = 3,
            ReceivedDateTime = DateTime.UtcNow
        };

        var subject = CreateSubject();

        var actual = subject.Map<BillRequestSent>(source);
        actual = subject.Map(entity, actual);

        Assert.Equal("HBA1CPOC", actual.ProductCode);
        Assert.Equal("HBA1CPOC", actual.BillingProductCode);
        Assert.Equal(source.CreatedDateTime, actual.CreateDate);
        Assert.Equal(entity.ProviderId, actual.ProviderId);
        Assert.Equal(entity.EvaluationId, actual.EvaluationId);
        Assert.Equal(entity.MemberPlanId, (int)actual.MemberPlanId);
    }

    [Fact]
    public void Map_From_Exam_To_BillRequestNotSent_Test()
    {
        var source = new Core.Data.Entities.HBA1CPOC
        {
            EvaluationId = 1,
            ProviderId = 2,
            MemberPlanId = 3,
            ReceivedDateTime = DateTime.UtcNow,
            CreatedDateTime = DateTimeOffset.UtcNow,
        };

        var subject = CreateSubject();

        var actual = subject.Map<BillRequestNotSent>(source);

        Assert.Equal("HBA1CPOC", actual.ProductCode);
        Assert.Equal("HBA1CPOC", actual.BillingProductCode);
        Assert.Equal(source.CreatedDateTime, actual.CreateDate);
        Assert.Equal(source.ReceivedDateTime, actual.ReceivedDate);
        Assert.Equal(source.ProviderId, actual.ProviderId);
        Assert.Equal(source.EvaluationId, actual.EvaluationId);
        Assert.Equal(source.MemberPlanId, (int)actual.MemberPlanId);
    }

    [Fact]
    public void Map_From_Entity_To_ResultsReceived_Tests()
    {
        var source = new Core.Data.Entities.HBA1CPOC
        {
            EvaluationId = 1,
            CreatedDateTime = DateTimeOffset.UtcNow,
            ReceivedDateTime = DateTime.UtcNow
        };

        var subject = CreateSubject();

        var actual = subject.Map<ResultsReceived>(source);

        Assert.Equal("HBA1CPOC", actual.ProductCode);
        Assert.Equal(1, actual.EvaluationId);
        Assert.Equal(source.CreatedDateTime, actual.PerformedDate);
        Assert.Equal(source.ReceivedDateTime, actual.ReceivedDate);
    }

    [Fact]
    public void Map_From_Normality_To_NormalityIndicator_IsMapped()
    {
        var source = Normality.Normal;

        var subject = CreateSubject();

        var actual = subject.Map<string>(source);

        Assert.NotNull(actual);
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
            HBA1CPOCId = 10000
        };

        // Act
        var actual = subject.Map<PDFToClient>(createOrUpdatePdfToClient);

        // Assert
        Assert.Equal(DateTimeKind.Utc, actual.DeliveryCreatedDateTime.Kind);
        Assert.Equal(DateTimeKind.Utc, actual.DeliveryDateTime.Kind);
    }
}