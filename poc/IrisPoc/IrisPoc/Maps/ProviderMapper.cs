using AutoMapper;
using Iris.Public.Types.Models;
using IrisPoc.Models;

namespace IrisPoc.Maps;

/// <summary>
/// Maps DEE's <see cref="ProviderModel"/> to the IRIS request model <see cref="RequestProvider"/>
/// </summary>
public class ProviderMapper : ITypeConverter<ProviderModel, RequestProvider>
{
    public RequestProvider Convert(ProviderModel source, RequestProvider destination, ResolutionContext context)
    {
        destination ??= new RequestProvider();

        destination.NPI = source.Npi;

        // We aren't sending the provider's email address over to their API, but the service bus implementation does support it
        destination.Email = source.Email;

        destination.Name = new PersonName
        {
            First = source.FirstName,
            Last = source.LastName
        };

        //source.ProviderId
        // Unlike the API that appears to have a ProviderId field, the service bus implementation has no concept of
        // a ProviderId or a LocalId

        return destination;
    }
}
