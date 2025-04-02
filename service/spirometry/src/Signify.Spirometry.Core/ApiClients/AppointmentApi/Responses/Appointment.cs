using System.Diagnostics.CodeAnalysis;

namespace Signify.Spirometry.Core.ApiClients.AppointmentApi.Responses;

/// <summary>
/// This is a subset of the full model, which can be found at
/// https://dev.azure.com/signifyhealth/HCC/_git/domain.appointment?path=/api/appointmentapi/src/AppointmentApi.Core/DTO/vw_AppointmentsV2Dto.cs&version=GBmaster
/// </summary>
[ExcludeFromCodeCoverage]
public class Appointment
{
    /// <summary>
    /// Identifier of the appointment
    /// </summary>
    public long AppointmentId { get; set; }

    /// <summary>
    /// Identifier of the member's healthcare plan
    /// </summary>
    public long MemberPlanId { get; set; }
}