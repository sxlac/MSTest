using Signify.Spirometry.Core.ApiClients.RcmApi.Requests;
using Signify.Spirometry.Core.Commands;
using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Maps;
using System;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Maps;

public class CreateBillRequestMapperTests
{
    [Fact]
    public void Convert_FromExam_WithNullDestination_ReturnsNotNull()
    {
        var subject = new CreateBillRequestMapper();

        var actual = subject.Convert(new SpirometryExam(), null, null);

        Assert.NotNull(actual);
    }

    [Fact]
    public void Convert_FromCreateBill_WithNullDestination_ReturnsNotNull()
    {
        var subject = new CreateBillRequestMapper();

        var actual = subject.Convert(new CreateBill(), null, null);

        Assert.NotNull(actual);
    }

    [Fact]
    public void Convert_FromExam_WithNullAdditionalDetails_InstantiatesAdditionalDetails()
    {
        var destination = new CreateBillRequest
        {
            AdditionalDetails = null
        };

        var subject = new CreateBillRequestMapper();

        subject.Convert(new SpirometryExam(), destination, null);

        Assert.NotNull(destination.AdditionalDetails);
    }

    [Fact]
    public void Convert_FromCreateBill_WithNullAdditionalDetails_InstantiatesAdditionalDetails()
    {
        var destination = new CreateBillRequest
        {
            AdditionalDetails = null
        };

        var subject = new CreateBillRequestMapper();

        subject.Convert(new CreateBill(), destination, null);

        Assert.NotNull(destination.AdditionalDetails);
    }

    [Fact]
    public void Convert_FromExam_SetsProperties()
    {
        var source = new SpirometryExam
        {
            State = "TX",
            ClientId = 1,
            ProviderId = 2,
            DateOfService = new DateTime(2022, 01, 02, 03, 04, 05, DateTimeKind.Utc),
            MemberPlanId = 3,
            AppointmentId = 4
        };

        var subject = new CreateBillRequestMapper();

        var actual = subject.Convert(source, null, null);

        Assert.Equal(source.State, actual.UsStateOfService);
        Assert.Equal(source.ClientId, actual.SharedClientId);
        Assert.Equal(source.ProviderId, actual.ProviderId);
        Assert.Equal(source.DateOfService, actual.DateOfService);
        Assert.Equal(source.MemberPlanId, actual.MemberPlanId);

        Assert.Equal("Signify.Spirometry.Svc", actual.ApplicationId);

        Assert.True(actual.AdditionalDetails.TryGetValue("appointmentId", out var appointmentId));
        Assert.Equal("4", appointmentId);
    }

    [Fact]
    public void Convert_FromCreateBill_SetsProperties()
    {
        var source = new CreateBill
        {
            BillableDate = new DateTime(2022, 01, 02, 03, 04, 05, DateTimeKind.Utc),
            BatchName = "BatchName",
            EvaluationId = 1
        };

        var subject = new CreateBillRequestMapper();

        var actual = subject.Convert(source, null, null);

        Assert.Equal(source.BillableDate, actual.BillableDate);

        Assert.True(actual.AdditionalDetails.TryGetValue("BatchName", out var batchName));
        Assert.Equal(source.BatchName, batchName);

        Assert.True(actual.AdditionalDetails.TryGetValue("EvaluationId", out var evaluationId));
        Assert.Equal("1", evaluationId);
    }
}