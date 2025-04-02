using AutoMapper;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Signify.FOBT.Svc.Core.BusinessRules;
using Signify.FOBT.Svc.Core.Commands;
using Signify.FOBT.Svc.Core.Constants;
using Signify.FOBT.Svc.Core.Models;
using Signify.FOBT.Svc.Core.Tests.Utilities;
using System.Threading.Tasks;
using Xunit;

namespace Signify.FOBT.Svc.Core.Tests.Commands;

public class CreateLabResultTests: IClassFixture<MockDbFixture>
{
    private readonly CreateLabResultHandler _handler;
    private readonly IMapper _mapper = A.Fake<IMapper>();
    private readonly IBillableRules _billableRules = A.Fake<IBillableRules>();

    public CreateLabResultTests(MockDbFixture mockDbFixture)
    {
        var logger = A.Fake<ILogger<CreateLabResultHandler>>();
        _handler = new CreateLabResultHandler(logger, mockDbFixture.Context, _mapper, _billableRules);
    }

    [Fact]
    public async Task Handler_LabResult_ResultAlreadyExists()
    {
        // Arrange
        A.CallTo(() => _billableRules.IsLabResultValid(A<BillableRuleAnswers>._)).Returns(new BusinessRuleStatus{IsMet = true});

        // Act
        var result = await _handler.Handle(new CreateLabResult{FOBTId = 1}, default);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Handler_LabResult_BillableRulesFalse()
    {
        // Arrange
        A.CallTo(() => _billableRules.IsLabResultValid(A<BillableRuleAnswers>._)).Returns(new BusinessRuleStatus{IsMet = false});

        // Act
        var result = await _handler.Handle(new CreateLabResult(), default);

        // Assert
        Assert.Equal(ApplicationConstants.UNDETERMINISTIC, result.AbnormalIndicator);
    }

    [Fact]
    public async Task Handler_LabResult_Undeterministic()
    {
        // Act
        var result = await _handler.Handle(new CreateLabResult(), default);

        // Assert
        Assert.Equal(ApplicationConstants.UNDETERMINISTIC, result.AbnormalIndicator);
    }
}