using System;
using AutoFixture;


namespace Signify.DEE.Svc.Core.Tests.Utilities;

public class EntityFixtures : IDisposable
{
    public readonly Fixture _fixture;

    public EntityFixtures()
    {
        _fixture = new Fixture();
    }
      
        
    public void Dispose()
    {
           
    }
}