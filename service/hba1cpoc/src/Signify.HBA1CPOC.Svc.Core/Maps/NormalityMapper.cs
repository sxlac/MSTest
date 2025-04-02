using AutoMapper;
using Signify.HBA1CPOC.Svc.Core.Models;
using System;
using Signify.HBA1CPOC.Svc.Core.Constants;

namespace Signify.HBA1CPOC.Svc.Core.Maps;

public class NormalityMapper : ITypeConverter<Normality, string>, ITypeConverter<string, Normality>
{
    public string Convert(Normality source, string destination, ResolutionContext context)
    {
        return source switch
        {
            Normality.Normal => NormalityIndicatorConstants.Normal,
            Normality.Abnormal => NormalityIndicatorConstants.Abnormal,
            Normality.Undetermined => NormalityIndicatorConstants.Undetermined,
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, "Unhandled normality")
        };
    }

    public Normality Convert(string source, Normality destination, ResolutionContext context)
    {
        return source switch
        {
            NormalityIndicatorConstants.Normal => Normality.Normal,
            NormalityIndicatorConstants.Abnormal => Normality.Abnormal,
            NormalityIndicatorConstants.Undetermined => Normality.Undetermined,
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, "Unhandled normality")
        };
    }
}