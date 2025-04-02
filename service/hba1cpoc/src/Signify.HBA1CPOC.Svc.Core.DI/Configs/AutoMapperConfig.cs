using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Signify.HBA1CPOC.Svc.Core.Maps;
using System;

namespace Signify.HBA1CPOC.Svc.Core.DI.Configs;

public static class AutoMapperConfig
{
    public static IMapper AddAutoMapper(IServiceCollection services)
    {
        var mappingConfig = new MapperConfiguration(mc =>
        {
            mc.CreateMap<DateTime, DateOnly>().ConvertUsing((s, _) => DateOnly.FromDateTime(s));
            mc.CreateMap<DateTime?, DateOnly?>().ConvertUsing((s, _) =>
            {
                if (s.HasValue)
                {
                    return DateOnly.FromDateTime(s.Value);
                }
                return null;
            });
            mc.CreateMap<DateTime, DateTime>().ConvertUsing((s, _) => DateTime.SpecifyKind(s, DateTimeKind.Utc));
            mc.CreateMap<DateTime?, DateTime?>().ConvertUsing((s, _) =>
            {
                if (s.HasValue)
                {
                    return DateTime.SpecifyKind(s.Value, DateTimeKind.Utc);
                }
                return null;
            });
            mc.CreateMap<DateTimeOffset, DateTimeOffset>().ConvertUsing((s, _) => s.ToUniversalTime());
            mc.CreateMap<DateTimeOffset?, DateTimeOffset?>().ConvertUsing((s, _) => s?.ToUniversalTime());

            mc.AddProfile<MappingProfile>();
            var sp = services.BuildServiceProvider();
            mc.ConstructServicesUsing(type =>
                ActivatorUtilities.CreateInstance(sp, type));
        });

        return mappingConfig.CreateMapper();
    }
}