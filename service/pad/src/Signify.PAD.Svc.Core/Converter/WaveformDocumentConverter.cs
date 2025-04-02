using System;
using Signify.PAD.Svc.Core.Validators;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Signify.PAD.Svc.Core.Converter
{
    public class WaveformDocumentConverter : IWaveformDocumentConverter
    {
        private readonly IStringValidator _sValidator;
        private const string Delimiter = "_";
        private const string DateFormat = "MMddyy";

        public WaveformDocumentConverter(IStringValidator sValidator)
        {
            _sValidator = sValidator;
        }

        public int ConvertMemberPlanId(string fileName)
        {
            if (!_sValidator.IsValid(fileName))
                throw new ArgumentNullException(nameof(fileName));

            if (!_sValidator.IsValid(fileName, Delimiter))
                throw new InvalidOperationException($"Waveform Document filename does not contain delimiter {Delimiter}.");
            
            var iMemberPlanId = fileName.Split(Delimiter).Where(s => !string.IsNullOrWhiteSpace(s))
                .FirstOrDefault(value => Regex.IsMatch(value, @"^-?[0-9]*$"));
            
            if (int.TryParse(iMemberPlanId, out var memberPlanId))
                return memberPlanId;
            throw new InvalidOperationException("Waveform Document filename does not contain MemberPlanId.");
        }

        public DateTime ConvertDateOfExam(string fileName)
        {
            if (!_sValidator.IsValid(fileName))
                throw new ArgumentNullException(nameof(fileName));

            if (!_sValidator.IsValid(fileName, Delimiter))
                throw new InvalidOperationException($"Waveform Document filename does not contain delimiter {Delimiter}.");
            
            var iDateOfExam = fileName.Split(Delimiter).Where(s => !string.IsNullOrWhiteSpace(s))
                .FirstOrDefault(value => Regex.IsMatch(value, @"^[0-9]{6}\.PDF$", RegexOptions.IgnoreCase))?[..DateFormat.Length];
            if (DateTime.TryParseExact(iDateOfExam, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateOfExam))
                return dateOfExam;
            throw new InvalidOperationException("Waveform Document filename does not contain a valid DateOfExam.");
        }
    }
}

