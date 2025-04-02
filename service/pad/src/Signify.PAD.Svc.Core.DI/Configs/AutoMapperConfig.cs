using System;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Signify.PAD.Svc.Core.Maps;

namespace Signify.PAD.Svc.Core.DI.Configs;

public static class AutoMapperConfig
{
    public static IMapper AddAutoMapper(IServiceCollection services)
    {
        var mappingConfig = new MapperConfiguration(mc =>
        {
            mc.CreateMap<DateTime, DateTime>().ConvertUsing((s, d) =>
            {
                return s.Kind == DateTimeKind.Local ?
                    DateTime.SpecifyKind(s, DateTimeKind.Local) :
                    DateTime.SpecifyKind(s, DateTimeKind.Utc);
            });
            mc.CreateMap<DateTime?, DateTime?>().ConvertUsing((s, d) =>
            {
                if (s.HasValue)
                {
                    return s.Value.Kind == DateTimeKind.Local ?
                        DateTime.SpecifyKind(s.Value, DateTimeKind.Local) :
                        DateTime.SpecifyKind(s.Value, DateTimeKind.Utc);
                }
                return null;
            });
            mc.CreateMap<DateTimeOffset, DateTimeOffset>().ConvertUsing((s, d) =>
            {
                return s.ToUniversalTime();
            });
            mc.CreateMap<DateTimeOffset?, DateTimeOffset?>().ConvertUsing((s, d) =>
            {
                return s.HasValue ? s.Value.ToUniversalTime() : null;
            });

            mc.AddProfile<MappingProfile>();
            var sp = services.BuildServiceProvider();
            mc.ConstructServicesUsing(type =>
                ActivatorUtilities.CreateInstance(sp, type));
        });

        return mappingConfig.CreateMapper();
    }
}