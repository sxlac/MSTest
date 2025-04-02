/* Replaced when SQL Server Database was migrated to Azure Database for PostgreSQL. */

--ANC-3621

-- Table: public.Configuration

-- DROP TABLE public."Configuration";

CREATE TABLE public."Configuration"
(
    "ConfigurationId" SERIAL PRIMARY KEY NOT NULL,
    "ConfigurationName" VARCHAR(256) NOT NULL,
    "ConfigurationValue" VARCHAR(256) NOT NULL,
    "LastUpdated" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);
 
ALTER TABLE public."Configuration" OWNER to flywaydee;
GRANT INSERT, SELECT, UPDATE ON TABLE public."Configuration" TO svcdee;

-- Table: public.Exam

-- DROP TABLE public."Exam";

CREATE TABLE public."Exam"
(
    "ExamId" SERIAL PRIMARY KEY NOT NULL,
    "DeeExamId" INTEGER,
    "EvaluationId" BIGINT,
    "MemberPlanId" BIGINT NOT NULL,
    "ProviderId" INTEGER NOT NULL,
    "DateOfService" TIMESTAMP WITH TIME ZONE NOT NULL,
    "Gradeable" BOOLEAN,
    "CreatedDateTime" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "State" VARCHAR(5),
    "RequestId" UUID,
    "ClientId" INTEGER,
    "ReceivedDateTime" TIMESTAMP WITH TIME ZONE,
    "ExamLocalId" VARCHAR(50) UNIQUE
);
 
ALTER TABLE public."Exam" OWNER to flywaydee;
GRANT INSERT, SELECT, UPDATE ON TABLE public."Exam" TO svcdee;

-- Table: public.ExamStatusCode

-- DROP TABLE public."ExamStatusCode";

CREATE TABLE public."ExamStatusCode"
(
    "ExamStatusCodeId" SERIAL PRIMARY KEY NOT NULL,
    "Name" VARCHAR(250) NOT NULL
);
 
ALTER TABLE public."ExamStatusCode" OWNER to flywaydee;
GRANT SELECT ON TABLE public."ExamStatusCode" TO svcdee;

INSERT INTO public."ExamStatusCode"
        ("Name")
VALUES  ('Exam Created'),
		('IRIS Awaiting Interpretation'),
		('IRIS Interpreted'),
		('Result Data Downloaded'),
		('PDF Data Downloaded'),
		('Sent To Billing'),
		('No DEE Images Taken'),
		('IRIS Image Received'),
		('Gradable'),
		('Not Gradable'),
		('DEE Images Found'),
		('IRIS Exam Created'),
		('IRIS Result Downloaded'),
		('PCP Letter Sent'),
		('No PCP Found'),
		('Member Letter Sent'),
		('Sent To Provider Pay'),
		('DEE Performed'),
		('DEE Not Performed'),
		('Billable Event Recieved'),
		('DEE Incomplete'),
		('Bill Request Not Sent'),
        ('ProviderPayableEventReceived'),
        ('ProviderNonPayableEventReceived'),
        ('ProviderPayRequestSent'),
        ('CdiPassedReceived'),
        ('CdiFailedWithPayReceived'),
        ('CdiFailedWithoutPayReceived');
		
-- Table: public.LateralityCode

-- DROP TABLE public."LateralityCode";

CREATE TABLE public."LateralityCode"
(
    "LateralityCodeId" SERIAL PRIMARY KEY NOT NULL,
    "Name" VARCHAR(12) NOT NULL UNIQUE,
    "Description" VARCHAR(256) NOT NULL
);
 
ALTER TABLE public."LateralityCode" OWNER to flywaydee;
GRANT SELECT ON TABLE public."LateralityCode" TO svcdee;

INSERT INTO public."LateralityCode"
		("Name", "Description")
VALUES	('OD', 'Right, Oculu'),
		('OS', 'Left, Oculus Sinster'),
		('OU', 'Both, Oculus Uterque'),
		('Unknown' ,'Unknown');
		
-- Table: public.NotPerformedReason

-- DROP TABLE public."NotPerformedReason";

CREATE TABLE public."NotPerformedReason"
(
    "NotPerformedReasonId" SERIAL PRIMARY KEY NOT NULL,
    "AnswerId" INTEGER NOT NULL,
    "Reason" VARCHAR(256) NOT NULL
);
 
ALTER TABLE public."NotPerformedReason" OWNER to flywaydee;
GRANT SELECT ON TABLE public."NotPerformedReason" TO svcdee;

INSERT INTO public."NotPerformedReason"
		("AnswerId", "Reason")
VALUES	(30943, 'Member recently completed'),
		(30944, 'Scheduled to complete'),
		(30945, 'Member apprehension'),
		(30946, 'Not interested'),
		(30947, 'Other'),
		(30950, 'Technical issue'),
		(30951, 'Environmental issue'),
		(30952, 'No supplies or equipment'),
		(30953, 'Insufficient training'),
		(50914, 'Member physically unable');

-- Table: public.ExamStatus

-- DROP TABLE public."ExamStatus";

CREATE TABLE public."ExamStatus"
(
    "ExamStatusId" SERIAL PRIMARY KEY NOT NULL,
    "ExamId" INTEGER NOT NULL REFERENCES "Exam" ("ExamId"),
    "CreatedDateTime" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "ReceivedDateTime" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "ExamStatusCodeId" INTEGER REFERENCES "ExamStatusCode" ("ExamStatusCodeId"),
    "DeeEventId" UUID
);
 
ALTER TABLE public."ExamStatus" OWNER to flywaydee;
GRANT INSERT, SELECT, UPDATE ON TABLE public."ExamStatus" TO svcdee;

-- Table: public.ExamResult

-- DROP TABLE public."ExamResult";

CREATE TABLE public."ExamResult"
(
    "ExamResultId" SERIAL PRIMARY KEY NOT NULL,
    "ExamId" INTEGER NOT NULL REFERENCES "Exam" ("ExamId"),
    "GradableImage" BOOLEAN NOT NULL DEFAULT false,
    "GraderFirstName" VARCHAR(50),
    "GraderLastName" VARCHAR(50),
    "GraderNpi" VARCHAR(10),
    "GraderTaxonomy" VARCHAR(50),
    "DateSigned" TIMESTAMP WITH TIME ZONE,
    "CarePlan" VARCHAR(500),
    "NormalityIndicator" VARCHAR(1),
    "LeftEyeHasPathology" BOOLEAN,
    "RightEyeHasPathology" BOOLEAN
);
 
ALTER TABLE public."ExamResult" OWNER to flywaydee;
GRANT INSERT, SELECT, UPDATE ON TABLE public."ExamResult" TO svcdee;

-- Table: public.ExamLateralityGrade

-- DROP TABLE public."ExamLateralityGrade";

CREATE TABLE public."ExamLateralityGrade"
(
    "ExamLateralityGradeId" SERIAL PRIMARY KEY NOT NULL,
    "ExamId" INTEGER NOT NULL REFERENCES "Exam" ("ExamId"),
    "LateralityCodeId" INTEGER NOT NULL REFERENCES "LateralityCode" ("LateralityCodeId"),
    "Gradable" BOOLEAN NOT NULL
);
 
ALTER TABLE public."ExamLateralityGrade" OWNER to flywaydee;
GRANT INSERT, SELECT, UPDATE ON TABLE public."ExamLateralityGrade" TO svcdee;

-- Table: public.ExamImage

-- DROP TABLE public."ExamImage";

CREATE TABLE public."ExamImage"
(
    "ExamImageId" SERIAL PRIMARY KEY NOT NULL,
    "ExamId" INTEGER NOT NULL REFERENCES "Exam" ("ExamId"),
    "DeeImageId" INTEGER,
    "ImageQuality" VARCHAR(15),
    "ImageType" VARCHAR(15),
    "LateralityCodeId" INTEGER REFERENCES "LateralityCode" ("LateralityCodeId"), /* CR: Add NOT NULL? */
    "Gradable" BOOLEAN,
    "NotGradableReasons" VARCHAR(1000),
    "ImageLocalId" VARCHAR(50) UNIQUE
);
 
ALTER TABLE public."ExamImage" OWNER to flywaydee;
GRANT INSERT, SELECT, UPDATE ON TABLE public."ExamImage" TO svcdee;

-- Table: public.ExamFinding

-- DROP TABLE public."ExamFinding";

CREATE TABLE public."ExamFinding"
(
    "ExamFindingId" SERIAL PRIMARY KEY NOT NULL,
    "ExamResultId" INTEGER NOT NULL REFERENCES "ExamResult" ("ExamResultId"),
    "LateralityCodeId" INTEGER REFERENCES "LateralityCode" ("LateralityCodeId"),
    "Finding" VARCHAR(500),
    "NormalityIndicator" VARCHAR(1)
);
 
ALTER TABLE public."ExamFinding" OWNER to flywaydee;
GRANT INSERT, SELECT, UPDATE ON TABLE public."ExamFinding" TO svcdee;

-- Table: public.ExamDiagnosis

-- DROP TABLE public."ExamDiagnosis";

CREATE TABLE public."ExamDiagnosis"
(
    "ExamDiagnosisId" SERIAL PRIMARY KEY NOT NULL,
    "ExamResultId" INTEGER NOT NULL REFERENCES "ExamResult" ("ExamResultId"),
    "Diagnosis" VARCHAR(50)
);
 
ALTER TABLE public."ExamDiagnosis" OWNER to flywaydee;
GRANT INSERT, SELECT, UPDATE ON TABLE public."ExamDiagnosis" TO svcdee;

-- Table: public.DEEBilling

-- DROP TABLE public."DEEBilling";

CREATE TABLE public."DEEBilling"
(
    "Id" SERIAL PRIMARY KEY NOT NULL,
    "BillId" VARCHAR(50),
    "ExamId" INTEGER NOT NULL REFERENCES "Exam" ("ExamId"),
    "CreatedDateTime" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);
 
ALTER TABLE public."DEEBilling" OWNER to flywaydee;
GRANT INSERT, SELECT, UPDATE ON TABLE public."DEEBilling" TO svcdee;

-- Table: public.DeeNotPerformed

-- DROP TABLE public."DeeNotPerformed";

CREATE TABLE public."DeeNotPerformed"
(
    "DeeNotPerformedId" SERIAL PRIMARY KEY NOT NULL,
    "ExamId" INTEGER NOT NULL REFERENCES "Exam" ("ExamId"),
    "NotPerformedReasonId" smallint NOT NULL REFERENCES "NotPerformedReason" ("NotPerformedReasonId"),
    "CreatedDateTime" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "Notes" TEXT
);
 
ALTER TABLE public."DeeNotPerformed" OWNER to flywaydee;
GRANT INSERT, SELECT, UPDATE ON TABLE public."DeeNotPerformed" TO svcdee;

-- Table: public.NonGradableReason

-- DROP TABLE public."NonGradableReason";

CREATE TABLE public."NonGradableReason"
(
    "NonGradableReasonId" SERIAL PRIMARY KEY NOT NULL,
    "ExamLateralityGradeId" INTEGER NOT NULL REFERENCES "ExamLateralityGrade" ("ExamLateralityGradeId"),
    "Reason" VARCHAR(1000) NOT NULL
);
 
ALTER TABLE public."NonGradableReason" OWNER to flywaydee;
GRANT INSERT, SELECT, UPDATE ON TABLE public."NonGradableReason" TO svcdee;

-- Table: public.PDFToClient

-- DROP TABLE public."PDFToClient";

CREATE TABLE public."PDFToClient"
(
    "PDFDeliverId" SERIAL PRIMARY KEY NOT NULL,
    "EventId" VARCHAR(40),
    "EvaluationId" BIGINT NOT NULL,
    "DeliveryDateTime" TIMESTAMP WITH TIME ZONE NOT NULL,
    "DeliveryCreatedDateTime" TIMESTAMP WITH TIME ZONE NOT NULL,
    "BatchId" BIGINT NOT NULL,
    "BatchName" VARCHAR(200),
    "ExamId" INTEGER NOT NULL REFERENCES "Exam" ("ExamId"),
    "CreatedDateTime" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);
 
ALTER TABLE public."PDFToClient" OWNER to flywaydee;
GRANT INSERT, SELECT, UPDATE ON TABLE public."PDFToClient" TO svcdee;

-- Table: public.ProviderPay

-- DROP TABLE IF EXISTS public."ProviderPay";

CREATE TABLE public."ProviderPay"
(
    "Id" SERIAL PRIMARY KEY NOT NULL,
    "PaymentId" VARCHAR(50),
    "ExamId" INTEGER NOT NULL REFERENCES "Exam" ("ExamId"),
    "CreatedDateTime" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

ALTER TABLE public."ProviderPay" OWNER to flywaydee;
GRANT INSERT, SELECT, UPDATE ON TABLE public."ProviderPay" TO svcdee;

-- Table: public.schema_version? What to do here, i.e does flyway create it's own table?

--All

GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO svcdee;

--Add Indexes:

-- Table: public.DEEBilling

CREATE INDEX IDX_DEEBilling_ExamId ON public."DEEBilling" ("ExamId");

-- Table: public.DeeNotPerformed

CREATE INDEX IDX_DeeNotPerformed_ExamId ON public."DeeNotPerformed" ("ExamId");
CREATE INDEX IDX_DeeNotPerformed_NotPerformedReasonId ON public."DeeNotPerformed" ("NotPerformedReasonId");

-- Table: public.Exam

CREATE INDEX IDX_Exam_EvaluationId ON public."Exam" ("EvaluationId");

-- Table: public.ExamDiagnosis

CREATE INDEX IDX_ExamDiagnosis_ExamResultId ON public."ExamDiagnosis" ("ExamResultId");

-- Table: public.ExamFinding

CREATE INDEX IDX_ExamFinding_ExamResultId ON public."ExamFinding" ("ExamResultId");
CREATE INDEX IDX_ExamFinding_LateralityCodeId ON public."ExamFinding" ("LateralityCodeId");

-- Table: public.ExamImage

CREATE INDEX IDX_ExamImage_ExamId ON public."ExamImage" ("ExamId");
CREATE INDEX IDX_ExamImage_DeeImageId ON public."ExamImage" ("DeeImageId");
CREATE INDEX IDX_ExamImage_LateralityCodeId ON public."ExamImage" ("LateralityCodeId");

-- Table: public.ExamLateralityGrade

CREATE INDEX IDX_ExamLateralityGrade_ExamId ON public."ExamLateralityGrade" ("ExamId");
CREATE INDEX IDX_ExamLateralityGrade_LateralityCodeId ON public."ExamLateralityGrade" ("LateralityCodeId");

-- Table: public.ExamResult

CREATE INDEX IDX_ExamResult_ExamId ON public."ExamResult" ("ExamId");

-- Table: public.ExamStatus

CREATE INDEX IDX_ExamStatus_ExamId ON public."ExamStatus" ("ExamId");
CREATE INDEX IDX_ExamStatus_ExamStatusCodeId ON public."ExamStatus" ("ExamStatusCodeId");

-- Table: public.NonGradableReason

CREATE INDEX IDX_NonGradableReason_ExamLateralityGradeId ON public."NonGradableReason" ("ExamLateralityGradeId");

-- Table: public.PDFToClient

CREATE INDEX IDX_PDFToClient_ExamId ON public."PDFToClient" ("ExamId");

-- Table: public.ProviderPay

CREATE INDEX IDX_ProviderPay_ExamId ON public."ProviderPay" ("ExamId");

/*IF OBJECT_ID('LateralityCode', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.LateralityCode (
        LateralityCodeId INT NOT NULL IDENTITY(1,1) CONSTRAINT [PK_LateralityCode] PRIMARY KEY CLUSTERED (LateralityCodeId ASC),
        [Name] NVARCHAR(12) NOT NULL CONSTRAINT [UQ_LateralityCode_Name] UNIQUE,
		[Description] VARCHAR (256) NOT NULL
    )

	INSERT INTO LateralityCode ([Name], [Description]) VALUES
	('OD', 'Right, Oculus Dexter'),
	('OS', 'Left, Oculus Sinster'),
	('OU', 'Both, Oculus Uterque'),
	('Unknown', 'Unknown')
END
GO

IF OBJECT_ID('Exam', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Exam (
        ExamId INT NOT NULL IDENTITY(1,1) CONSTRAINT [PK_Exam] PRIMARY KEY CLUSTERED (ExamId ASC),
        DeeExamId INT null,
        EvaluationId INT NOT NULL,
        MemberPlanId BIGINT NOT NULL,
        ProviderId INT NOT NULL,
        DateOfService DATETIME NOT NULL,
		Gradeable BIT NULL,
		CreatedDateTime DATETIME NOT NULL CONSTRAINT DF_Exam_CreateDateTime DEFAULT GETDATE()
    )
END
GO

IF OBJECT_ID('ExamStatusCode', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ExamStatusCode (
        ExamStatusCodeId INT NOT NULL IDENTITY(1,1) CONSTRAINT [PK_ExamStatusCode] PRIMARY KEY CLUSTERED (ExamStatusCodeId ASC),
        [Name] VARCHAR(250) NOT NULL
    )
	
	INSERT INTO ExamStatusCode ([Name]) VALUES
	('Exam Created'),
	('Awaiting Interpretation'),
	('Interpreted'),
	('Results Downloaded'),
	('PDF Created'),
	('Sent To Billing'),
	('No Images Taken')
END
GO

IF OBJECT_ID('ExamStatus', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ExamStatus (
        ExamStatusId INT NOT NULL IDENTITY(1,1) CONSTRAINT [PK_ExamStatus] PRIMARY KEY CLUSTERED (ExamStatusId ASC),
        ExamId INT NOT NULL CONSTRAINT FK_ExamStatus_Exam FOREIGN KEY(ExamId) REFERENCES Exam(ExamId),
        CreatedDateTime DATETIME NOT NULL,
        ReceivedDateTime DATETIME NOT NULL CONSTRAINT DF_ExamStatus_ReceivedDateTime DEFAULT GETDATE(),
        ExamStatusCodeId INT CONSTRAINT FK_ExamStatus_ExamStatusCode FOREIGN KEY(ExamStatusCodeId) REFERENCES ExamStatusCode(ExamStatusCodeId),
        DeeEventId UNIQUEIDENTIFIER NULL,
    )
END
GO

IF OBJECT_ID('ExamResult', 'U') IS NULL
BEGIN
   CREATE TABLE dbo.ExamResult(
      ExamResultId INT NOT NULL IDENTITY(1,1) CONSTRAINT PK_ExamResult PRIMARY KEY CLUSTERED (ExamResultid ASC),
      ExamId INT NOT NULL CONSTRAINT FK_ExamResult_Exam FOREIGN KEY(ExamId) REFERENCES Exam(ExamId),
      GradableImage BIT NOT NULL CONSTRAINT DF_ExamResult_GradableImage DEFAULT 0,
      GraderFirstName NVARCHAR(50) NULL,
      GraderLastName NVARCHAR(50) NULL,
      GraderNpi NVARCHAR(10) NULL,
      GraderTaxonomy NVARCHAR(50) NULL,
      DateSigned DATETIME NULL,
      CarePlan NVARCHAR(500) NULL
   )
END
GO

IF OBJECT_ID('ExamFinding', 'U') IS NULL
BEGIN
   CREATE TABLE dbo.ExamFinding(
       ExamFindingId INT NOT NULL IDENTITY(1,1) CONSTRAINT PK_ExamFinding PRIMARY KEY CLUSTERED (ExamFindingid ASC),
       ExamResultId INT NOT NULL CONSTRAINT FK_ExamFinding_ExamResult FOREIGN KEY(ExamResultId) REFERENCES dbo.ExamResult(ExamResultId),
       LateralityCodeId INT NULL CONSTRAINT FK_ExamFinding_LateralityCode FOREIGN KEY(LateralityCodeId) REFERENCES [dbo].LateralityCode (LateralityCodeId),
       Finding NVARCHAR(500) NULL
   )
END
GO

IF OBJECT_ID('ExamDiagnosis', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ExamDiagnosis(
        ExamDiagnosisId INT NOT NULL IDENTITY(1,1) CONSTRAINT PK_ExamDiagnosis PRIMARY KEY CLUSTERED (ExamDiagnosisId ASC),
        ExamResultId INT NOT NULL CONSTRAINT FK_ExamDiagnosis_ExamResult FOREIGN KEY(ExamResultId) REFERENCES [dbo].ExamResult (ExamResultId),
        Diagnosis NVARCHAR(50) NULL
    )
END
GO

IF OBJECT_ID('ExamImage', 'U') IS NULL
BEGIN
	CREATE TABLE dbo.ExamImage(
		ExamImageId INT NOT NULL IDENTITY(1,1) CONSTRAINT PK_ExamImage PRIMARY KEY CLUSTERED (ExamImageId ASC),
		ExamId INT NOT NULL CONSTRAINT FK_ExamImage_Exam FOREIGN KEY(ExamId) REFERENCES Exam(ExamId),
		DeeImageId INT NULL,
		ImageQuality NVARCHAR(15) NULL,
		ImageType NVARCHAR(15) NULL,
		LateralityCodeId INT NULL CONSTRAINT FK_ExamImage_LateralityCode FOREIGN KEY(LateralityCodeId) REFERENCES [dbo].LateralityCode (LateralityCodeId)
	)
END
GO*/