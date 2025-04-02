using System;
using AutoMapper;
using Signify.PAD.Svc.Core.Commands;
using Signify.PAD.Svc.Core.Converter;
using Signify.PAD.Svc.Core.Data.Entities;

namespace Signify.PAD.Svc.Core.Maps
{
    public class WaveformDocumentMapper : ITypeConverter<ProcessPendingWaveform, WaveformDocument>
    {
        private readonly IWaveformDocumentConverter _waveformDocumentConverter;

        public WaveformDocumentMapper(IWaveformDocumentConverter waveformDocumentConverter)
        {
            _waveformDocumentConverter = waveformDocumentConverter;
        }

        public WaveformDocument Convert(ProcessPendingWaveform source, WaveformDocument destination, ResolutionContext context)
        {
            if (destination == null)
                destination = new WaveformDocument();

            destination.WaveformDocumentVendorId = source.Vendor.WaveformDocumentVendorId;
            destination.Filename = source.Filename;
            destination.MemberPlanId = MemberPlanIdResolver(source.Filename);
            destination.DateOfExam = DateOfExamResolver(source.Filename);

            return destination;
        }

        private int MemberPlanIdResolver(string filename)
            => _waveformDocumentConverter.ConvertMemberPlanId(filename);

        private DateTime DateOfExamResolver(string filename)
            => _waveformDocumentConverter.ConvertDateOfExam(filename);
    }
}
