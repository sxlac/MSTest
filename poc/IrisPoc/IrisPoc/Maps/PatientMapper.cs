using AutoMapper;
using Iris.Public.Types.Enums;
using Iris.Public.Types.Models;
using IrisPoc.Models;

namespace IrisPoc.Maps;

/// <summary>
/// Maps DEE's <see cref="PatientModel"/> to the IRIS request model <see cref="RequestPatient"/>
/// </summary>
public class PatientMapper : ITypeConverter<PatientModel, RequestPatient>
{
    public RequestPatient Convert(PatientModel source, RequestPatient destination, ResolutionContext context)
    {
        destination ??= new RequestPatient();

        switch (source.Gender)
        {
            case "M":
            case "Male":
                destination.Gender = Gender.M;
                break;
            case "F":
            case "Female":
                destination.Gender = Gender.F;
                break;
        }

        destination.Name = new PersonName
        {
            First = source.FirstName,
            Last = source.LastName
        };

        destination.LocalId = source.PatientId.ToString(); // Corresponds to MemberPlanId in DEE
        destination.Dob = source.BirthDate.ToString("MM/dd/yyyy");

        // Only the properties set above are required according to the documentation

        //source.State
        // The destination does have an Address property in which we can specify the member's US State,
        // but, although we're sending this to the API, I'm not sure why IRIS would really need this,
        // and I'm not sure if their Address model supports only specifying State and no other address
        // details. 

        return destination;
    }
}