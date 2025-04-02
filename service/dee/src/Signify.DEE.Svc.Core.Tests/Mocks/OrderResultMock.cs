using Iris.Public.Types.Enums;
using Iris.Public.Types.Models;
using Iris.Public.Types.Models.Common;
using Iris.Public.Types.Models.V2_3_1;
using System;
using System.Collections.Generic;

namespace Signify.DEE.Svc.Core.Tests.Mocks;

public static class OrderResultMock
{
    public static OrderResult BuildOrderResult()
    {
        return new OrderResult
        {
            Version = "2.3.1",
            TransactionId = Guid.NewGuid().ToString(),
            ResultCode = "F",                   // P = Preliminary, A= Addendum, F=Final, C=Correction, R=Resend
            Site = new Iris.Public.Types.Models.ResultSite
            {
                LocalId = "PC1234"
            },
            ResultsDocument = new Iris.Public.Types.Models.ResultsDocument
            {
                Type = "PDF",                   // PDF, HTML, ORU
                Encoding = "Base64",            // Base64, HL7, or a specific encoding scheme
                Content = "..."                 // Actual Content
            },
            ImageDetails = new Iris.Public.Types.Models.ResultImageDetails
            {
                TotalCount = 2,
                RightEyeCount = 1,
                RightEyeOriginalCount = 0,
                RightEyeEnhancedCount = 0,
                LeftEyeCount = 1,
                LeftEyeOriginalCount = 0,
                LeftEyeEnhancedCount = 0,
                SingleEyeOnly = false           // If true the order expects images from one eye side only
            },
            Images = new List<ResultImage>
            {
                new ResultImage
                {
                    Taken = DateTimeOffset.Now.AddHours(-1),        // Timestamp when Image was taken
                    Received = DateTimeOffset.Now.AddHours(-1),     // Timestamp when Image was received by Iris
                    FileName = $"{Guid.NewGuid()}.jpg",               // Filename of Image
                    Laterality = Laterality.OD,                     // OS, OD
                    ImageContext = ImageContext.Primary,            // Primary, Secondary, Component, Aggregate, Enhancement
                    Camera = BuildBaseCamera(),
                    LocalId = "1"
                },
                new ResultImage
                {
                    Taken = DateTimeOffset.Now.AddHours(-1),
                    Received = DateTimeOffset.Now.AddHours(-1),
                    FileName = $"{Guid.NewGuid()}.jpg",
                    Laterality = Laterality.OS,
                    ImageContext = ImageContext.Primary,
                    Camera = BuildBaseCamera(),
                    LocalId = "2"
                }
            },
            Order = new ResultOrder
            {
                Status = "Complete",
                LocalId = "ORD1234",
                PatientOrderID = 100123,
                EvaluationTypes = BuildEvaluationTypes(),           // DR, Glaucoma, HIV
                CreatedTime = DateTimeOffset.Now.AddDays(-4),       // Timestamp when order was created
                ServicedTime = DateTimeOffset.Now.AddDays(-1),      // Timestamp when exam was performed
                State = "FL",                                       // Optional US State of Order (Required for patient home exams)
                SingleEyeOnly = false,                              // If true the order expects images from one eye side only
                Expedite = false                                    // If true the order needs priority routing to next available gender
            },
            Patient = new ResultPatient
            {
                LocalId = "1234",                                   // Medical record number (or any Id unique to the patient for the submitting organization)
                Name = new PersonName
                {
                    First = "Jim",
                    Last = "Smith",
                },
                Dob = "1/1/1960",
                Gender = Gender.U
            },
            OrderingProvider = new ResultProvider
            {
                NPI = "1234567890",
                Taxonomy = "207W00000X",
                Name = new PersonName
                {
                    First = "Frank",
                    Last = "Johnson"
                },
                Email = "frank.johnson@providers.com",
                Degrees = "MD"
            },
            Gradings = new ResultGrading
            {
                Notes = new List<Note>
                {
                    new Note
                    {
                        Date = DateTimeOffset.Now.AddHours(-1),
                        Text = "Note"
                    }
                },
                GradedTime = DateTimeOffset.Now.AddHours(-1),                                   // Timestamp of Grading
                CarePlanName = "Return in 6 months",
                CarePlanDescription = "Have patient return in 6 months for a followup exam.",
                Pathology = true,                                                               // If true, pathology was found
                Urgent = false,                                                                 // If true, grader found urgent pathology to act on immediately
                OD = new ResultEyeSideGrading
                {
                    Gradable = true,                                                            // If true the grader was able to make an assessment based on one or more of the provided images
                    UngradableReasons = new List<string> { },                                   // If ungradable, contains a list of reasons provided by the grader.  These are not specific to any image
                    Findings = new List<ResultFinding>
                    {
                        new ResultFinding
                        {
                            Finding = "Diabetic Retinopathy",
                            Result = "Mild"
                        }
                    }
                },
                OS = new ResultEyeSideGrading
                {
                    Gradable = true,
                    Findings = new List<ResultFinding>
                    {
                        new ResultFinding
                        {
                            Finding = "Other",
                            Result = "Severe"
                        }
                    }
                },
                DiagnosisCodes = new List<DiagnosisCode>
                {
                    new DiagnosisCode
                    {
                        Code = "E083211"
                    }
                },
                Provider = new ResultProvider                                                   // Provider who graded the order
                {
                    NPI = "1234567890",
                    Taxonomy = "207W00000X",
                    Name = new PersonName
                    {
                        First = "John",
                        Last = "Doe"
                    },
                    Email = "john.doe@providers.com",
                    Degrees = "MD"
                }
            },
            CameraOperator = new ResultCameraOperator                 // Technician who captured images
            {
                UserName = "jake.thomas@primarycare.com",
                Name = new PersonName
                {
                    First = "Jake",
                    Last = "Thomas"
                },
                Notes = new List<Note>
                {
                    new Note
                    {
                        Date = DateTimeOffset.Now.AddHours(-1),
                        Text = string.Empty
                    }
                }
            },
            HealthPlan = new ResultHealthPlan
            {
                LocalId = "1234",                                       // Id of plan as specified by submitting organization
                Name = "ProviderA",                                     // Name of the healthplan
                MemberId = "1234"                                       // Id of Memmber as specified by the Healthplan
            }
        };
    }

    public static OrderResult BuildOrderResultWithCustomResults(List<ResultFinding> OdFindings, List<ResultFinding> OsFindings)
    {
        return new OrderResult
        {
            Version = "2.3.1",
            TransactionId = Guid.NewGuid().ToString(),
            ResultCode = "F",                   // P = Preliminary, A= Addendum, F=Final, C=Correction, R=Resend
            Site = new Iris.Public.Types.Models.ResultSite
            {
                LocalId = "PC1234"
            },
            ResultsDocument = new Iris.Public.Types.Models.ResultsDocument
            {
                Type = "PDF",                   // PDF, HTML, ORU
                Encoding = "Base64",            // Base64, HL7, or a specific encoding scheme
                Content = "..."                 // Actual Content
            },
            ImageDetails = new Iris.Public.Types.Models.ResultImageDetails
            {
                TotalCount = 2,
                RightEyeCount = 1,
                RightEyeOriginalCount = 0,
                RightEyeEnhancedCount = 0,
                LeftEyeCount = 1,
                LeftEyeOriginalCount = 0,
                LeftEyeEnhancedCount = 0,
                SingleEyeOnly = false           // If true the order expects images from one eye side only
            },
            Images = new List<ResultImage>
            {
                new ResultImage
                {
                    Taken = DateTimeOffset.Now.AddHours(-1),        // Timestamp when Image was taken
                    Received = DateTimeOffset.Now.AddHours(-1),     // Timestamp when Image was received by Iris
                    FileName = $"{Guid.NewGuid()}.jpg",               // Filename of Image
                    Laterality = Laterality.OD,                     // OS, OD
                    ImageContext = ImageContext.Primary,            // Primary, Secondary, Component, Aggregate, Enhancement
                    Camera = BuildBaseCamera()
                },
                new ResultImage
                {
                    Taken = DateTimeOffset.Now.AddHours(-1),
                    Received = DateTimeOffset.Now.AddHours(-1),
                    FileName = $"{Guid.NewGuid()}.jpg",
                    Laterality = Laterality.OS,
                    ImageContext = ImageContext.Primary,
                    Camera = BuildBaseCamera()
                }
            },
            Order = new ResultOrder
            {
                Status = "Complete",
                LocalId = "ORD1234",
                PatientOrderID = 100123,
                EvaluationTypes = BuildEvaluationTypes(),           // DR, Glaucoma, HIV
                CreatedTime = DateTimeOffset.Now.AddDays(-4),       // Timestamp when order was created
                ServicedTime = DateTimeOffset.Now.AddDays(-1),      // Timestamp when exam was performed
                State = "FL",                                       // Optional US State of Order (Required for patient home exams)
                SingleEyeOnly = false,                              // If true the order expects images from one eye side only
                Expedite = false                                    // If true the order needs priority routing to next available gender
            },
            Patient = new ResultPatient
            {
                LocalId = "1234",                                   // Medical record number (or any Id unique to the patient for the submitting organization)
                Name = new PersonName
                {
                    First = "Jim",
                    Last = "Smith",
                },
                Dob = "1/1/1960",
                Gender = Gender.U
            },
            OrderingProvider = new ResultProvider
            {
                NPI = "1234567890",
                Taxonomy = "207W00000X",
                Name = new PersonName
                {
                    First = "Frank",
                    Last = "Johnson"
                },
                Email = "frank.johnson@providers.com",
                Degrees = "MD"
            },
            Gradings = new ResultGrading
            {
                Notes = new List<Note>
                {
                    new Note
                    {
                        Date = DateTimeOffset.Now.AddHours(-1),
                        Text = "Note"
                    }
                },
                GradedTime = DateTimeOffset.Now.AddHours(-1),                                   // Timestamp of Grading
                CarePlanName = "Return in 6 months",
                CarePlanDescription = "Have patient return in 6 months for a followup exam.",
                Pathology = true,                                                               // If true, pathology was found
                Urgent = false,                                                                 // If true, grader found urgent pathology to act on immediately
                OD = new ResultEyeSideGrading
                {
                    Gradable = true,                                                            // If true the grader was able to make an assessment based on one or more of the provided images
                    UngradableReasons = new List<string> { },                                   // If ungradable, contains a list of reasons provided by the grader.  These are not specific to any image
                    Findings = OdFindings
                },
                OS = new ResultEyeSideGrading
                {
                    Gradable = true,
                    Findings = OsFindings
                },
                DiagnosisCodes = new List<DiagnosisCode>
                {
                    new DiagnosisCode
                    {
                        Code = "E083211"
                    }
                },
                Provider = new ResultProvider                                                   // Provider who graded the order
                {
                    NPI = "1234567890",
                    Taxonomy = "207W00000X",
                    Name = new PersonName
                    {
                        First = "John",
                        Last = "Doe"
                    },
                    Email = "john.doe@providers.com",
                    Degrees = "MD"
                }
            },
            CameraOperator = new ResultCameraOperator                 // Technician who captured images
            {
                UserName = "jake.thomas@primarycare.com",
                Name = new PersonName
                {
                    First = "Jake",
                    Last = "Thomas"
                },
                Notes = new List<Note>
                {
                    new Note
                    {
                        Date = DateTimeOffset.Now.AddHours(-1),
                        Text = string.Empty
                    }
                }
            },
            HealthPlan = new ResultHealthPlan
            {
                LocalId = "1234",                                       // Id of plan as specified by submitting organization
                Name = "ProviderA",                                     // Name of the healthplan
                MemberId = "1234"                                       // Id of Memmber as specified by the Healthplan
            }
        };
    }

    public static OrderResult BuildOrderResultWithEnucleation()
    {
        return new OrderResult
        {
            Version = "2.3.1",
            TransactionId = Guid.NewGuid().ToString(),
            ResultCode = "F",                   // P = Preliminary, A= Addendum, F=Final, C=Correction, R=Resend
            Site = new Iris.Public.Types.Models.ResultSite
            {
                LocalId = "PC1234"
            },
            ResultsDocument = new Iris.Public.Types.Models.ResultsDocument
            {
                Type = "PDF",                   // PDF, HTML, ORU
                Encoding = "Base64",            // Base64, HL7, or a specific encoding scheme
                Content = "..."                 // Actual Content
            },
            ImageDetails = new Iris.Public.Types.Models.ResultImageDetails
            {
                TotalCount = 1,
                RightEyeCount = 0,
                RightEyeOriginalCount = 0,
                RightEyeEnhancedCount = 0,
                LeftEyeCount = 1,
                LeftEyeOriginalCount = 0,
                LeftEyeEnhancedCount = 0,
                SingleEyeOnly = true           // If true the order expects images from one eye side only
            },
            Images = new List<ResultImage>
            {                
                new ResultImage
                {
                    Taken = DateTimeOffset.Now.AddHours(-1),
                    Received = DateTimeOffset.Now.AddHours(-1),
                    FileName = $"{Guid.NewGuid()}.jpg",
                    Laterality = Laterality.OS,
                    ImageContext = ImageContext.Primary,
                    Camera = BuildBaseCamera(),
                    LocalId = "2"
                }
            },
            Order = new ResultOrder
            {
                Status = "Complete",
                LocalId = "ORD1234",
                PatientOrderID = 100123,
                EvaluationTypes = BuildEvaluationTypes(),           // DR, Glaucoma, HIV
                CreatedTime = DateTimeOffset.Now.AddDays(-4),       // Timestamp when order was created
                ServicedTime = DateTimeOffset.Now.AddDays(-1),      // Timestamp when exam was performed
                State = "FL",                                       // Optional US State of Order (Required for patient home exams)
                SingleEyeOnly = true,                              // If true the order expects images from one eye side only
                Expedite = false,                                    // If true the order needs priority routing to next available gender
                MissingEyeReason = "Enculeation"
            },
            Patient = new ResultPatient
            {
                LocalId = "1234",                                   // Medical record number (or any Id unique to the patient for the submitting organization)
                Name = new PersonName
                {
                    First = "Jim",
                    Last = "Smith",
                },
                Dob = "1/1/1960",
                Gender = Gender.U
            },
            OrderingProvider = new ResultProvider
            {
                NPI = "1234567890",
                Taxonomy = "207W00000X",
                Name = new PersonName
                {
                    First = "Frank",
                    Last = "Johnson"
                },
                Email = "frank.johnson@providers.com",
                Degrees = "MD"
            },
            Gradings = new ResultGrading
            {
                Notes = new List<Note>
                {
                    new Note
                    {
                        Date = DateTimeOffset.Now.AddHours(-1),
                        Text = "Note"
                    }
                },
                GradedTime = DateTimeOffset.Now.AddHours(-1),                                   // Timestamp of Grading
                CarePlanName = "Return in 6 months",
                CarePlanDescription = "Have patient return in 6 months for a followup exam.",
                Pathology = true,                                                               // If true, pathology was found
                Urgent = false,                                                                 // If true, grader found urgent pathology to act on immediately
                OD = new ResultEyeSideGrading
                {
                    Gradable = false,                                                            // If true the grader was able to make an assessment based on one or more of the provided images
                    UngradableReasons = new List<string> { "No image provided" },
                    MissingEyeReason = "Enucleation",
                    Findings = new List<ResultFinding>
                    {                        
                    }
                },
                OS = new ResultEyeSideGrading
                {
                    Gradable = true,
                    Findings = new List<ResultFinding>
                    {
                        new ResultFinding
                        {
                            Finding = "Other",
                            Result = "Severe"
                        }
                    }
                },
                DiagnosisCodes = new List<DiagnosisCode>
                {
                    new DiagnosisCode
                    {
                        Code = "E083211"
                    }
                },
                Provider = new ResultProvider                                                   // Provider who graded the order
                {
                    NPI = "1234567890",
                    Taxonomy = "207W00000X",
                    Name = new PersonName
                    {
                        First = "John",
                        Last = "Doe"
                    },
                    Email = "john.doe@providers.com",
                    Degrees = "MD"
                }
            },
            CameraOperator = new ResultCameraOperator                 // Technician who captured images
            {
                UserName = "jake.thomas@primarycare.com",
                Name = new PersonName
                {
                    First = "Jake",
                    Last = "Thomas"
                },
                Notes = new List<Note>
                {
                    new Note
                    {
                        Date = DateTimeOffset.Now.AddHours(-1),
                        Text = string.Empty
                    }
                }
            },
            HealthPlan = new ResultHealthPlan
            {
                LocalId = "1234",                                       // Id of plan as specified by submitting organization
                Name = "ProviderA",                                     // Name of the healthplan
                MemberId = "1234"                                       // Id of Memmber as specified by the Healthplan
            }
        };
    }

    private static BaseCamera BuildBaseCamera()
    {
        return new BaseCamera
        {
            LocalId = "REM-1000",           // Specifies Camera Id as defined by submitting organization
            SerialNumber = "REM-1000",      // Serial Number of Camera
            Manufacturer = "Remidio",       // Manufacturer of Camera
            Model = "NMFOP",                // Model Name of Camera
            SoftwareVersion = "2.2.23"
        };
    }

    private static EvaluationType[] BuildEvaluationTypes()
    {
        var evaluationTypes = new EvaluationType[1];
        evaluationTypes[0] = EvaluationType.DR;

        return evaluationTypes;
    }
}