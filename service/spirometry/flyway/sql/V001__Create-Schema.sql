--Note - DB is created with terraform, not flyway.

CREATE TABLE public."SpirometryExam"
(
    "SpirometryExamId" SERIAL PRIMARY KEY NOT NULL,
    "ApplicationId" VARCHAR(200) NULL,
    "EvaluationId" INTEGER NULL UNIQUE,
    "ProviderId" INTEGER NULL,
    "MemberId" BIGINT NULL,
    "MemberPlanId" INTEGER NULL,
    "CenseoId" VARCHAR(8) NULL,
    "AppointmentId" INTEGER NULL,
    "DateOfService" TIMESTAMP WITH TIME ZONE NULL, -- From evaluation event, when the provider gave the service
    "FirstName" VARCHAR(100) NULL,
    "MiddleName" VARCHAR(100) NULL,
    "LastName" VARCHAR(100) NULL,
    "DateOfBirth" TIMESTAMP NULL,
    "AddressLineOne" VARCHAR(200) NULL,
    "AddressLineTwo" VARCHAR(200) NULL,
    "City" VARCHAR(100) NULL,
    "State" VARCHAR(2) NULL,
    "ZipCode" VARCHAR(5) NULL,
    "NationalProviderIdentifier" VARCHAR(10) NULL,
    "EvaluationReceivedDateTime" TIMESTAMP WITH TIME ZONE NOT NULL, -- From evaluation event, when the evaluation was received by the Evaluation API
    "EvaluationCreatedDateTime" TIMESTAMP WITH TIME ZONE NOT NULL, -- From Evaluation event, when the evaluation was first started/created
    "CreatedDateTime" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW() -- Now; when the Spirometry process manager received this event
);

ALTER TABLE public."SpirometryExam" OWNER TO flywayspirometry;
GRANT SELECT, UPDATE, INSERT ON public."SpirometryExam" TO svcspirometry;

CREATE INDEX IDX_SpirometryExam_EvaluationId ON public."SpirometryExam" ("EvaluationId");

--

CREATE TABLE public."StatusCode"
(
    "StatusCodeId" SERIAL PRIMARY KEY,
    "Name" VARCHAR(250) NOT NULL UNIQUE
);

ALTER TABLE public."StatusCode" OWNER TO flywayspirometry;
GRANT SELECT ON public."StatusCode" TO svcspirometry;

INSERT INTO public."StatusCode"
        ("Name")
VALUES  ('Spirometry Exam Performed'),
        ('Spirometry Exam Not Performed'),
        ('Billable Event Received'),
        ('Bill Request Sent');

--

CREATE TABLE public."ExamStatus" -- Corresponds to status events published to "spirometry_status" Kafka topic
(
    "ExamStatusId" SERIAL PRIMARY KEY NOT NULL,
    "SpirometryExamId" INTEGER NOT NULL REFERENCES "SpirometryExam" ("SpirometryExamId"),
    "StatusCodeId" INTEGER NOT NULL REFERENCES "StatusCode" ("StatusCodeId"),
    "StatusDateTime" TIMESTAMP WITH TIME ZONE NOT NULL, -- The date and time when the status changed
    "CreateDateTime" TIMESTAMP WITH TIME ZONE NULL DEFAULT NOW() -- The date and time when this notification was created within the Spirometry process manager
);

ALTER TABLE public."ExamStatus" OWNER TO flywayspirometry;
GRANT SELECT, UPDATE, INSERT ON public."ExamStatus" TO svcspirometry;

CREATE INDEX IDX_ExamStatus_SpirometryExamId ON public."ExamStatus" ("SpirometryExamId");
CREATE INDEX IDX_ExamStatus_StatusCodeId ON public."ExamStatus" ("StatusCodeId");

--

-- A Trilean is similar to a Boolean, except it has a third possible value - Unknown.
-- In many cases, using a nullable boolean (both in code and db) is sufficient, but
-- in the case of the spirometry evaluation answers we are recording that need to
-- support this third value, there is a conceptual difference between the db type NULL
-- and 'Unknown'. NULL would correspond to an evaluation answer we have no knowledge of
-- (ie the provider did not answer that question), versus them choosing an answer that
-- is specifically one of 'Yes', 'No' and 'Unknown'.
CREATE TABLE public."TrileanType"
(
    "TrileanTypeId" SMALLSERIAL PRIMARY KEY NOT NULL,
    "TrileanValue" VARCHAR(8) NOT NULL UNIQUE
);

ALTER TABLE public."TrileanType" OWNER TO flywayspirometry;
GRANT SELECT ON public."TrileanType" TO svcspirometry;

INSERT INTO public."TrileanType"
        ("TrileanValue")
VALUES  ('Unknown'),
        ('Yes'),
        ('No');

--

-- A Session Grade is a rating of the quality of the examination. Poor quality grades
-- cannot be used to accurately diagnose for COPD.
CREATE TABLE public."SessionGrade"
(
    "SessionGradeId" SMALLSERIAL PRIMARY KEY NOT NULL,
    "SessionGradeCode" VARCHAR(8) NOT NULL UNIQUE,
    "IsGradable" BOOLEAN NOT NULL
);

ALTER TABLE public."SessionGrade" OWNER TO flywayspirometry;
GRANT SELECT ON public."SessionGrade" TO svcspirometry;

INSERT INTO public."SessionGrade"
        ("SessionGradeCode", "IsGradable")
VALUES  ('A', TRUE),
        ('B', TRUE),
        ('C', TRUE),
        ('D', FALSE),
        ('E', FALSE),
        ('F', FALSE);

--

-- Results for spirometry exams that were performed
CREATE TABLE public."SpirometryExamResults"
(
    "SpirometryExamResultsId" SERIAL PRIMARY KEY NOT NULL,
    "SpirometryExamId" INTEGER NOT NULL UNIQUE REFERENCES "SpirometryExam" ("SpirometryExamId"),
    "SessionGradeId" SMALLINT NOT NULL REFERENCES "SessionGrade" ("SessionGradeId"),
    "FVC" SMALLINT NOT NULL,
    "FEV1" SMALLINT NOT NULL,
    "FEV1_Over_FVC" NUMERIC(3,2) NOT NULL, -- x.xx
    "HasHighSymptomTrileanTypeId" SMALLINT NOT NULL REFERENCES "TrileanType" ("TrileanTypeId"),
    "HasEnvOrExpRiskTrileanTypeId" SMALLINT NOT NULL REFERENCES "TrileanType" ("TrileanTypeId"), -- Environment or Exposure risk
    "HasHighComorbidityTrileanTypeId" SMALLINT NOT NULL REFERENCES "TrileanType" ("TrileanTypeId"),
    "CopdDiagnosis" BOOLEAN NULL, -- COPD Diagnosis from the assessment. TRUE is a positive COPD diagnosis; NULL is cannot be determined.
    "CreatedDateTime" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

ALTER TABLE public."SpirometryExamResults" OWNER TO flywayspirometry;
GRANT SELECT, UPDATE, INSERT ON public."SpirometryExamResults" TO svcspirometry;

-- All reasons why an evaluation with a spirometry product may not be performed
CREATE TABLE public."NotPerformedReason"
(
    "NotPerformedReasonId" SMALLSERIAL PRIMARY KEY NOT NULL,
    "AnswerId" INTEGER NOT NULL UNIQUE,
    "Reason" VARCHAR(256) NOT NULL UNIQUE
);

ALTER TABLE public."NotPerformedReason" OWNER TO flywayspirometry;
GRANT SELECT ON public."NotPerformedReason" TO svcspirometry;

INSERT  INTO public."NotPerformedReason"
        ("AnswerId", "Reason")
VALUES  (50923, 'Member recently completed'),
        (50924, 'Scheduled to complete'),
        (50925, 'Member apprehension'),
        (50926, 'Not interested'),
        (50928, 'Technical issue'),
		(50929, 'Environment issue'),
		(50930, 'No supplies or equipment'),
		(50931, 'Insufficient training'),
		(50932, 'Member physically unable');

-- Details about evaluations where a spirometry exam was not performed
CREATE TABLE public."ExamNotPerformed"
(
    "ExamNotPerformedId" SERIAL PRIMARY KEY NOT NULL,
    "SpirometryExamId" INTEGER NOT NULL UNIQUE REFERENCES "SpirometryExam" ("SpirometryExamId"),
    "NotPerformedReasonId" SMALLINT NOT NULL REFERENCES "NotPerformedReason" ("NotPerformedReasonId"),
    "CreatedDateTime" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

ALTER TABLE public."ExamNotPerformed" OWNER TO flywayspirometry;
GRANT SELECT, UPDATE, INSERT ON public."ExamNotPerformed" TO svcspirometry;

GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO svcspirometry;
