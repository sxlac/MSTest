using Signify.uACR.Core.ApiClients.RcmApi.Requests;
using Signify.uACR.Core.Data.Entities;
using Signify.uACR.Core.Maps;
using System;
using UacrNsbEvents;
using Xunit;

namespace Signify.uACR.Core.Tests.Maps;

public class CreateBillRequestMapperTests
{
    [Fact]
    public void Convert_FromExam_WithNullDestination_ReturnsNotNull()
    {
        //Arrange
        var subject = new CreateBillRequestMapper();

        //Act
        var actual = subject.Convert(new Exam(), null, null);

        //Assert
        Assert.NotNull(actual);
    }

    [Fact]
    public void Convert_FromCreateBill_WithNullDestination_ReturnsNotNull()
    {
        //Arrange
        var subject = new CreateBillRequestMapper();

        //Act
        var actual = subject.Convert(new CreateBillEvent(), null, null);

        //Assert
        Assert.NotNull(actual);
    }

    [Fact]
    public void Convert_FromExam_WithNullAdditionalDetails_InstantiatesAdditionalDetails()
    {
        //Arrange
        var destination = new CreateBillRequest
        {
            AdditionalDetails = null
        };

        //Act
        var subject = new CreateBillRequestMapper();
        subject.Convert(new Exam(), destination, null);

        //Assert
        Assert.NotNull(destination.AdditionalDetails);
    }

    [Fact]
    public void Convert_FromCreateBill_WithNullAdditionalDetails_InstantiatesAdditionalDetails()
    {
        //Arrange
        var destination = new CreateBillRequest
        {
            AdditionalDetails = null
        };

        //Act
        var subject = new CreateBillRequestMapper();
        subject.Convert(new CreateBillEvent(), destination, null);

        //Assert
        Assert.NotNull(destination.AdditionalDetails);
    }

    [Fact]
    public void Convert_FromExam_SetsProperties()
    {
        //Arrange
        var source = new Exam
        {
            State = "TX",
            ClientId = 1,
            ProviderId = 2,
            DateOfService = new DateTime(2022, 01, 02, 03, 04, 05, DateTimeKind.Utc),
            MemberPlanId = 3,
            AppointmentId = 4
        };

        //Act
        var subject = new CreateBillRequestMapper();
        var actual = subject.Convert(source, null, null);

        //Assert
        Assert.Equal(source.State, actual.UsStateOfService);
        Assert.Equal(source.ClientId, actual.SharedClientId);
        Assert.Equal(source.ProviderId, actual.ProviderId);
        Assert.Equal(source.DateOfService, actual.DateOfService);
        Assert.Equal(source.MemberPlanId, actual.MemberPlanId);
        Assert.True(actual.AdditionalDetails.TryGetValue("appointmentId", out var appointmentId));
        Assert.Equal("4", appointmentId);
    }

    [Fact]
    public void Convert_FromCreateBill_SetsProperties()
    {
        //Arrange
        var source = new CreateBillEvent
        {
            BillableDate = new DateTime(2022, 01, 02, 03, 04, 05, DateTimeKind.Utc),
            BatchName = "BatchName",
            EvaluationId = 1
        };

        //Act
        var subject = new CreateBillRequestMapper();
        var actual = subject.Convert(source, null, null);

        //Assert
        Assert.Equal(source.BillableDate, actual.BillableDate);
        Assert.True(actual.AdditionalDetails.TryGetValue("BatchName", out var batchName));
        Assert.Equal(source.BatchName, batchName);
        Assert.True(actual.AdditionalDetails.TryGetValue("EvaluationId", out var evaluationId));
        Assert.Equal("1", evaluationId);
    }
}