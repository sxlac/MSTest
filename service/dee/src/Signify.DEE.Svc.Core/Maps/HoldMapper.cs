using AutoMapper;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.Events;
using Signify.DEE.Svc.Core.Filters;
using Signify.DEE.Svc.Core.Infrastructure;
using System.Linq;

namespace Signify.DEE.Svc.Core.Maps
{
    public class HoldMapper : ITypeConverter<CDIEvaluationHeldEvent, Hold>
    {
        private readonly IApplicationTime _applicationTime;
        private readonly IProductFilter _filter;

        public HoldMapper(IApplicationTime applicationTime, IProductFilter filter)
        {
            _applicationTime = applicationTime;
            _filter = filter;
        }

        public Hold Convert(CDIEvaluationHeldEvent source, Hold destination, ResolutionContext context)
        {
            destination ??= new Hold();

            destination.CdiHoldId = source.HoldId;
            destination.EvaluationId = source.EvaluationId;

            var productHold = source.Products.First(each => _filter.ShouldProcess(each));
            destination.ExpiresAt = productHold.ExpiresAt.UtcDateTime;

            destination.HeldOnDateTime = source.HeldOn.UtcDateTime;
            destination.SentAtDateTime = source.SentAt.UtcDateTime;

            destination.CreatedDateTime = _applicationTime.UtcNow();

            return destination;
        }
    }
}
