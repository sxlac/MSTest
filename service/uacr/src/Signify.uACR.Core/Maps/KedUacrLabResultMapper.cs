using System;
using AutoMapper;
using Hl7.Fhir.Model;
using System.Linq;
using System.Text.Json;
using Hl7.Fhir.Serialization;
using Signify.uACR.Core.Events;
using Signify.uACR.Core.Exceptions;

namespace Signify.uACR.Core.Maps;

public class KedUacrLabResultMapper : ITypeConverter<JsonElement, KedUacrLabResult>
{
    public KedUacrLabResult Convert(JsonElement source, KedUacrLabResult destination, ResolutionContext context)
    {
        ParseFhirObject(source, out var bundle);
        
        destination ??= new KedUacrLabResult();

        destination.EvaluationId = ResolvePatient(bundle);
        destination.UrineAlbuminToCreatinineRatioResultDescription = ResolveObservationInterpretationText(bundle, destination.EvaluationId);
        destination.UacrResult = ResolveObservationCode(bundle, destination.EvaluationId); 

        return destination;
    }
    
    
    private static long ResolvePatient(Bundle source)
    {
        var diagnosticReport = DiagnosticReportFinder.FindDiagnosticReport(source);

        var patient = PatientFinder.FindPatient(diagnosticReport);
        
        if (!long.TryParse(patient.Identifier.Find(id => id?.System == "http://ihr.signify.com/participantId")?.Value, out var evaluationId))
        {
            throw new FhirParsePatientException("Patient: No ParticipantId found in the DiagnosticReport.");
        }
        return evaluationId;
    }
    
    private static string ResolveObservationInterpretationText(Bundle source, long evaluationId)
    {
        var diagnosticReport = DiagnosticReportFinder.FindDiagnosticReport(source);
        
        var observation = ObservationFinder.FindObservation(diagnosticReport, Constants.Fhir.CreatinineInUrineLoincCode, evaluationId);
        
        return observation.Interpretation?.FirstOrDefault()?.Text;
    }
    
    private static decimal? ResolveObservationCode(Bundle source, long evaluationId)
    {
        var diagnosticReport = DiagnosticReportFinder.FindDiagnosticReport(source);
        
        var observation = ObservationFinder.FindObservation(diagnosticReport, Constants.Fhir.CreatinineInUrineLoincCode, evaluationId);
        
        // validate unit of measurement 
        if (observation?.Value is Quantity { Value: not null } quantity) 
        {
            // validate the unit is mg/g
            if (!string.Equals(quantity.Code, "mg/g", StringComparison.OrdinalIgnoreCase))
               throw new FhirParseObservationException("Observation: Invalid units:"+quantity.Code, evaluationId);
            return (decimal)quantity.Value;
        }
        
        return null;
    }
    
    public static bool ParseFhirObject(JsonElement json, out Bundle bundle)
    {
        var parser = new FhirJsonParser();
        bundle = parser.Parse<Bundle>(json.ToString());

        return true;
    }
    
}
internal static class DiagnosticReportFinder
{
    public static DiagnosticReport FindDiagnosticReport(Bundle bundle)
    {
        var diagnosticReport = bundle.Entry
            .Select(e => e.Resource)
            .OfType<DiagnosticReport>()
            .FirstOrDefault();
        
        if (diagnosticReport == null)
        {
            throw new FhirParseDiagnosticReportException("DiagnosticReport: No DiagnosticReport found in the Bundle.");
        }

        return diagnosticReport;
    }
} 

internal static class ObservationFinder
{
    public static Observation FindObservation(DiagnosticReport diagnosticReport, string code, long evaluationId)
    {
        var observation = diagnosticReport.Contained
            .OfType<Observation>()
            .FirstOrDefault(obs => obs.Code?.Coding?.Exists(c => c.Code == code) == true && obs.Note.FirstOrDefault()?.Text != null);
        
        if (observation == null)
            throw new FhirParseObservationException("Observation: No matching observation found or observation value is null.", evaluationId);

        return observation;
    }
}

internal static class PatientFinder
{
    public static Patient FindPatient(DomainResource diagnosticReport)
    {
        var patient = diagnosticReport.Contained
            .OfType<Patient>()
            .FirstOrDefault();
        
        if (patient == null)
        {
            throw new FhirParsePatientException("Patient: No Patient found in the DiagnosticReport.");
        }

        return patient;
    }
}