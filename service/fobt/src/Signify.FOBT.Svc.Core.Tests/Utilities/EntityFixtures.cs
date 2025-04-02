using AutoFixture;
using Signify.FOBT.Messages.Events;
using Signify.FOBT.Svc.Core.ApiClient.Requests;
using Signify.FOBT.Svc.Core.ApiClient.Response;
using Signify.FOBT.Svc.Core.Data.Entities;
using Signify.FOBT.Svc.Core.Queries;
using System;

namespace Signify.FOBT.Svc.Core.Tests.Utilities;

public class EntityFixtures : IDisposable
{
    public readonly Fixture _fixture;

    public EntityFixtures()
    {
        _fixture = new Fixture();
    }

    public ProviderInfoRs MockProviderRs()
    {
        return _fixture.Build<ProviderInfoRs>().Create();
    }
    public FOBTStatus MockFOBTStatus()
    {
        return _fixture.Build<FOBTStatus>().Create();
    }

    public UpdateInventoryRequest MockUpdateInventory()
    {
        return _fixture.Build<UpdateInventoryRequest>().Create();
    }

    public FOBTPerformedEvent MockFOBTPerformed()
    {
        return _fixture.Build<FOBTPerformedEvent>().Create();
    }

    public QueryFOBTResponse MockQueryFOBTResponse()
    {
        return _fixture.Build<QueryFOBTResponse>().Create();
    }

    public void Dispose()
    {

    }
}