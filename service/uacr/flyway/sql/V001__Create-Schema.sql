-- uACR Exam Table
CREATE TABLE public."Exam"
(
    "ExamId" SERIAL PRIMARY KEY NOT NULL,
    "EvaluationId" BIGINT NOT NULL UNIQUE,
    "ApplicationId" VARCHAR(200) NOT NULL,
    "ProviderId" INTEGER NOT NULL,
    "MemberId" BIGINT NOT NULL,
    "MemberPlanId" BIGINT NOT NULL,
    "CenseoId" VARCHAR(8) NULL,
    "AppointmentId" BIGINT NOT NULL,
    "ClientId" INTEGER NOT NULL, -- Needed when we start to integrate with RCM
    "DateOfService" DATE NULL, -- From evaluation event, when the provider gave the service
    "FirstName" VARCHAR(100) NULL, -- Do we need to maintain this data?
    "MiddleName" VARCHAR(100) NULL, -- Do we need to maintain this data?
    "LastName" VARCHAR(100) NULL, -- Do we need to maintain this data?
    "DateOfBirth" TIMESTAMP NULL, -- Do we need to maintain this data?
    "AddressLineOne" VARCHAR(200) NULL, -- Do we need to maintain this data?
    "AddressLineTwo" VARCHAR(200) NULL, -- Do we need to maintain this data?
    "City" VARCHAR(100) NULL, -- Do we need to maintain this data?
    "State" VARCHAR(2) NULL, -- Do we need to maintain this data?
    "ZipCode" VARCHAR(5) NULL, -- Do we need to maintain this data?
    "NationalProviderIdentifier" VARCHAR(10) NULL, -- Do we need to maintain this data
    "EvaluationReceivedDateTime" TIMESTAMP WITH TIME ZONE NOT NULL, -- From evaluation event, when the evaluation was received by the Evaluation API
    "EvaluationCreatedDateTime" TIMESTAMP WITH TIME ZONE NOT NULL, -- From Evaluation event, when the evaluation was first started/created
    "CreatedDateTime" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW() -- Now; when process manager received this event
);

ALTER TABLE public."Exam" OWNER TO flywayuacr;
GRANT SELECT, UPDATE, INSERT ON public."Exam" TO svcuacr;

CREATE INDEX IDX_Exam_EvaluationId ON public."Exam" ("EvaluationId");

-- Evaluation Status Code Table
CREATE TABLE public."ExamStatusCode"
(
    "ExamStatusCodeId" SERIAL PRIMARY KEY,
    "StatusName" VARCHAR(250) NOT NULL UNIQUE
);

ALTER TABLE public."ExamStatusCode" OWNER TO flywayuacr;
GRANT SELECT ON public."ExamStatusCode" TO svcuacr;

INSERT INTO public."ExamStatusCode"
        ("StatusName")
VALUES  ('Exam Performed'),
        ('Exam Not Performed'),
        ('Billable Event Received'),
        ('Bill Request Sent'),
        ('Client PDF Delivered');

-- Evaluation Status Table
CREATE TABLE public."ExamStatus" -- 
(
    "ExamStatusId" SERIAL PRIMARY KEY NOT NULL,
    "ExamId" INTEGER NOT NULL REFERENCES "Exam" ("ExamId"),
    "ExamStatusCodeId" INTEGER NOT NULL REFERENCES "ExamStatusCode" ("ExamStatusCodeId"),
    "StatusDateTime" TIMESTAMP WITH TIME ZONE NOT NULL,
    "CreatedDateTime" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW() -- The date and time when this status was created within the process manager
);

ALTER TABLE public."ExamStatus" OWNER TO flywayuacr;
GRANT SELECT, INSERT ON public."ExamStatus" TO svcuacr;

CREATE INDEX IDX_ExamStatus_ExamId ON public."ExamStatus" ("ExamId");
CREATE INDEX IDX_ExamStatus_ExamStatusCodeId ON public."ExamStatus" ("ExamStatusCodeId");

-- Bill Request Table
CREATE TABLE public."BillRequest"
(
    "BillRequestId" SERIAL PRIMARY KEY NOT NULL,
    "ExamId" INTEGER NOT NULL REFERENCES "Exam" ("ExamId"),
    "BillId" UUID NOT NULL UNIQUE,
    "CreatedDateTime" TIMESTAMP WITH TIME ZONE NULL DEFAULT NOW() -- The date and time when this result was created within the process manager
);

ALTER TABLE public."BillRequest" OWNER TO flywayuacr;
GRANT SELECT, INSERT ON public."BillRequest" TO svcuacr;

CREATE INDEX IDX_BillRequest_ExamId ON public."BillRequest" ("ExamId");

-- BarcodeHistory
CREATE TABLE public."BarcodeHistory"
(
	"BarcodeHistoryId" SERIAL PRIMARY KEY,
    "ExamId" INTEGER NOT NULL REFERENCES "Exam"("ExamId"),
    "Barcode" VARCHAR(200) UNIQUE NOT NULL,
	"CreatedDateTime" TIMESTAMP WITH TIME ZONE NOT NULL
);

ALTER TABLE public."BarcodeHistory" OWNER TO flywayuacr;
GRANT SELECT, INSERT ON public."BarcodeHistory" TO svcuacr;

CREATE INDEX IDX_BarcodeHistory_ExamId ON public."BarcodeHistory" ("ExamId");

-- All reasons why an evaluation with a uACR product may not be performed
CREATE TABLE public."NotPerformedReason"
(
    "NotPerformedReasonId" SMALLSERIAL PRIMARY KEY NOT NULL,
    "AnswerId" INTEGER NOT NULL UNIQUE,
    "Reason" VARCHAR(256) NOT NULL UNIQUE
);

ALTER TABLE public."NotPerformedReason" OWNER TO flywayuacr;
GRANT SELECT ON public."NotPerformedReason" TO svcuacr;

-- INSERT  INTO public."NotPerformedReason"
--         ("AnswerId", "Reason")
-- VALUES  (0, 'Member recently completed'),
--         (0, 'Scheduled to complete'),
--         (0, 'Member apprehension'),
--         (0, 'Not interested'),
--         (0, 'Technical issue'),
-- 		(0, 'Environmental issue'),
-- 		(0, 'No supplies or equipment'),
-- 		(0, 'Insufficient training');

-- Details about evaluations where a uACR exam was not performed
CREATE TABLE public."ExamNotPerformed"
(
    "ExamNotPerformedId" SERIAL PRIMARY KEY NOT NULL,
    "ExamId" INTEGER NOT NULL UNIQUE REFERENCES "Exam" ("ExamId"),
    "NotPerformedReasonId" SMALLINT NOT NULL REFERENCES "NotPerformedReason" ("NotPerformedReasonId"),
    "CreatedDateTime" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

ALTER TABLE public."ExamNotPerformed" OWNER TO flywayuacr;
GRANT SELECT, INSERT ON public."ExamNotPerformed" TO svcuacr;

CREATE INDEX IDX_ExamNotPerformed_NotPerformedReasonId ON public."ExamNotPerformed" ("NotPerformedReasonId");

CREATE TABLE public."PdfDeliveredToClient"
(
    "PdfDeliveredToClientId" SERIAL PRIMARY KEY NOT NULL,
    "EventId" UUID NOT NULL,
    "EvaluationId" BIGINT NOT NULL,
    "BatchId" BIGINT NOT NULL,
    "BatchName" VARCHAR(256) NULL,
    "DeliveryDateTime" TIMESTAMP WITH TIME ZONE NOT NULL,
    "CreatedDateTime" TIMESTAMP WITH TIME ZONE NOT NULL -- When the PDF was created within the other Signify PDF service
);

ALTER TABLE public."PdfDeliveredToClient" OWNER TO flywayuacr;
GRANT SELECT, INSERT ON public."PdfDeliveredToClient" TO svcuacr;

CREATE INDEX IDX_PdfDeliveredToClient_EvaluationId ON public."PdfDeliveredToClient" ("EvaluationId");

GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO svcuacr;
