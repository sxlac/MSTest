-- Egfr Exam Table
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
    "DateOfService" TIMESTAMP WITH TIME ZONE NULL, -- From evaluation event, when the provider gave the service
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

ALTER TABLE public."Exam" OWNER TO flywayegfr;
GRANT SELECT, UPDATE, INSERT ON public."Exam" TO svcegfr;

CREATE INDEX IDX_Exam_EvaluationId ON public."Exam" ("EvaluationId");

-- Evaluation Status Code Table
CREATE TABLE public."ExamStatusCode"
(
    "ExamStatusCodeId" SERIAL PRIMARY KEY,
    "StatusName" VARCHAR(250) NOT NULL UNIQUE
);

ALTER TABLE public."ExamStatusCode" OWNER TO flywayegfr;
GRANT SELECT ON public."ExamStatusCode" TO svcegfr;

INSERT INTO public."ExamStatusCode"
        ("StatusName")
VALUES  ('Exam Performed'),
        ('Exam Not Performed'),
        ('Billable Event Received'),
        ('Bill Request Sent');

-- Evaluation Status Table
CREATE TABLE public."ExamStatus" -- 
(
    "ExamStatusId" SERIAL PRIMARY KEY NOT NULL,
    "ExamId" INTEGER NOT NULL REFERENCES "Exam" ("ExamId"),
    "ExamStatusCodeId" INTEGER NOT NULL REFERENCES "ExamStatusCode" ("ExamStatusCodeId"),
    "StatusDateTime" TIMESTAMP WITH TIME ZONE NOT NULL,
    "CreatedDateTime" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW() -- The date and time when this status was created within the process manager
);

ALTER TABLE public."ExamStatus" OWNER TO flywayegfr;
GRANT SELECT, UPDATE, INSERT ON public."ExamStatus" TO svcegfr;

CREATE INDEX IDX_ExamStatus_ExamId ON public."ExamStatus" ("ExamId");
CREATE INDEX IDX_ExamStatus_ExamStatusCodeId ON public."ExamStatus" ("ExamStatusCodeId");

-- Bill Request Table
CREATE TABLE public."BillRequest"
(
    "BillRequestId" SERIAL PRIMARY KEY NOT NULL,
    "BillId" UUID NOT NULL UNIQUE,
    "CreatedDateTime" TIMESTAMP WITH TIME ZONE NULL DEFAULT NOW(), -- The date and time when this result was created within the process manager
    "ExamId" INTEGER NOT NULL REFERENCES "Exam" ("ExamId")
);

ALTER TABLE public."BillRequest" OWNER TO flywayegfr;
GRANT SELECT, UPDATE, INSERT ON public."BillRequest" TO svcegfr;

CREATE INDEX IDX_BillRequest_ExamId ON public."BillRequest" ("ExamId");

-- BarcodeHistory
CREATE TABLE "BarcodeHistory"
(
	"BarcodeHistoryId" SERIAL PRIMARY KEY,
    "ExamId" INTEGER NOT NULL REFERENCES "Exam"("ExamId"),
    "Barcode" VARCHAR(200) UNIQUE NOT NULL,
	"CreatedDateTime" TIMESTAMP WITH TIME ZONE NOT NULL
);

ALTER TABLE public."BarcodeHistory" OWNER TO flywayegfr;
GRANT SELECT, UPDATE, INSERT ON public."BarcodeHistory" TO svcegfr;

CREATE INDEX IDX_BarcodeHistory_ExamId ON public."BarcodeHistory" ("ExamId");

-- All reasons why an evaluation with a egfr product may not be performed
CREATE TABLE public."NotPerformedReason"
(
    "NotPerformedReasonId" SMALLSERIAL PRIMARY KEY NOT NULL,
    "AnswerId" INTEGER NOT NULL UNIQUE,
    "Reason" VARCHAR(256) NOT NULL UNIQUE
);

ALTER TABLE public."NotPerformedReason" OWNER TO flywayegfr;
GRANT SELECT ON public."NotPerformedReason" TO svcegfr;

INSERT  INTO public."NotPerformedReason"
        ("AnswerId", "Reason")
VALUES  (51272, 'Member recently completed'),
        (51273, 'Scheduled to complete'),
        (51274, 'Member apprehension'),
        (51275, 'Not interested'),
        (51266, 'Technical issue'),
		(51267, 'Environmental issue'),
		(51268, 'No supplies or equipment'),
		(51269, 'Insufficient training');

-- Details about evaluations where a egfr exam was not performed
CREATE TABLE public."ExamNotPerformed"
(
    "ExamNotPerformedId" SERIAL PRIMARY KEY NOT NULL,
    "ExamId" INTEGER NOT NULL UNIQUE REFERENCES "Exam" ("ExamId"),
    "NotPerformedReasonId" SMALLINT NOT NULL REFERENCES "NotPerformedReason" ("NotPerformedReasonId"),
    "CreatedDateTime" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

ALTER TABLE public."ExamNotPerformed" OWNER TO flywayegfr;
GRANT SELECT, UPDATE, INSERT ON public."ExamNotPerformed" TO svcegfr;

CREATE INDEX IDX_ExamNotPerformed_NotPerformedReasonId ON public."ExamNotPerformed" ("NotPerformedReasonId");

GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO svcegfr;
