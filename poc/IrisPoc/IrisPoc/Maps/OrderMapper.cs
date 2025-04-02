using AutoMapper;
using Iris.Public.Types.Enums;
using Iris.Public.Types.Models;
using Iris.Public.Types.Models.V2_3_1;
using IrisPoc.Models;
using IrisPoc.Settings;
using Microsoft.Extensions.Options;

namespace IrisPoc.Maps;

/// <summary>
/// Maps DEE's <see cref="ExamModel"/> to the IRIS order creation request model <see cref="OrderRequest"/>
/// </summary>
public class OrderMapper : ITypeConverter<ExamModel, OrderRequest>
{
    private readonly IrisSettings _settings;

    public OrderMapper(IOptions<IrisSettings> options)
    {
        _settings = options.Value;
    }

    public OrderRequest Convert(ExamModel source, OrderRequest destination, ResolutionContext context)
    {
        destination ??= new OrderRequest();

        destination.OrderControlCode = OrderControlCode.NW; // "New Order"

        // Unique identifier for Signify Health
        destination.ClientGuid = _settings.ClientGuid.ToString();
        destination.Site = new RequestSite
        {
            // Identifier of our site. Signify has a single site in each environment, and the identifier just so
            // happens to have the same value in QA vs production. This field is required when submitting orders.
            LocalId = _settings.SiteLocalId
        };

        destination.Order = new RequestOrder
        {
            // Set the Order ID to the MemberPlanId (yes, I don't believe this is ideal, but this is what DEE is doing as of today)
            //LocalId = source.Patient!.PatientId.ToString(),

            // US State where the images are being taken. According to the docs, this is required for our use-case.
            // Comes from the answer to QuestionID 89331 in DEE
            State = source.State,

            // This isn't something we specify in the API, but I assume IRIS knows which (or all) of these evaluation
            // types we want performed.
            //
            // Possible values:
            // - DR (Diabetic Retinopathy and Macular Edema)
            // - Glaucoma
            // - HIV (HIV Retinopathy)
            // - Unknown
            EvaluationTypes = new [] {EvaluationType.DR}, // We currently only do DR here for Signify

            SingleEyeOnly = false // Look what we have! "If true, the order will not delay in the image pipeline after images are received on one eye"

            // Urgent = true/false // This is another property that may be beneficial to leverage - "If true the order is prioritized in the grading queue"
        };

        //source.DateOfService
        // Although IRIS's API appears to accept DOS as a parameter, I don't see anything like this in their service
        // bus models, so I suspect the "Service Date" they display in their order manager will be the date the order
        // was created. There is, though, a Taken (DateTimeOffset) property on their image model, fwiw.

        destination.Patient = context.Mapper.Map<RequestPatient>(source.Patient);

        destination.OrderingProvider = context.Mapper.Map<RequestProvider>(source.Provider);

        return destination;
    }
}
