using AutoFixture;
using Signify.A1C.Svc.Core.Data.Entities;
using Signify.A1C.Svc.Core.ApiClient.Response;
using Signify.A1C.Svc.Core.Commands;
using System;
using Signify.A1C.Messages.Events;
using Signify.A1C.Svc.Core.Sagas;
using Signify.A1C.Svc.Core.Queries;
using Signify.A1C.Svc.Core.ApiClient.Requests;
using Signify.A1C.Svc.Core.Infrastructure;

namespace Signify.A1C.Svc.Core.Tests.Utilities
{
    public class EntityFixtures : IDisposable
    {
        public readonly Fixture _fixture;

        public EntityFixtures()
        {
            _fixture = new Fixture();
        }
        public A1CEvaluationReceived MockEvalReceived()
        {
            return _fixture.Build<A1CEvaluationReceived>().Create();
        }

        public ProviderInfoRs MockProviderRs()
        {
            return _fixture.Build<ProviderInfoRs>().Create();
        }
        public A1CStatus MockA1CStatus()
        {
            return _fixture.Build<A1CStatus>().Create();
        }
        public A1CPerformedEvent MockA1CPerformed()
        {
            return _fixture.Build<A1CPerformedEvent>().Create();
        }

        public InventoryUpdateReceived MockUpdateInventory()
        {
            return _fixture.Build<InventoryUpdateReceived>().Create();
        }
        public UpdateInventoryRequest MockInventoryUpdateRequest()
        {
            return _fixture.Build<UpdateInventoryRequest>().Create();
        }
        public CreateOrUpdateA1C MockCreateOrUpdateA1C()
        {
            return _fixture.Build<CreateOrUpdateA1C>().Create();
        }

        public QueryA1CResponse MockQueryA1CResponse()
        {
            return _fixture.Build<QueryA1CResponse>().Create();
        }

        public OktaClientCredentialsHttpClientHandler MockOktaHandler()
        {
            return _fixture.Build<OktaClientCredentialsHttpClientHandler>().Create();
        }

        public void Dispose()
        {

        }
    }
}
