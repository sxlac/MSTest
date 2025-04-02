using Refit;
using Signify.Spirometry.Core.ApiClients.AppointmentApi.Responses;
using System.Threading.Tasks;

namespace Signify.Spirometry.Core.ApiClients.AppointmentApi
{
    /// <summary>
    /// Interface to make requests to the Signify Scheduling API
    ///
    /// See https://dev.azure.com/signifyhealth/HCC/_git/coreservices?path=/api/signify.schedulingapi.webapi/src/Signify.SchedulingApi.WebApi/Controllers/
    /// </summary>
    public interface IAppointmentApi
    {
        /// <summary>
        /// Gets an appointment by its AppointmentId
        /// </summary>
        /// <param name="appointmentId"></param>
        /// <returns></returns>
        [Get("/appointment/{appointmentId}")]
        [Headers("Authorization: Bearer")]
        Task<IApiResponse<Appointment>> GetAppointment(long appointmentId);
    }
}
