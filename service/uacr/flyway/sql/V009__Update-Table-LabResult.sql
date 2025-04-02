DROP INDEX IF EXISTS IDX_LabResult_CenseoId;

DROP TABLE public."LabResult";

CREATE TABLE public."LabResult"
(
    "LabResultId" SERIAL PRIMARY KEY NOT NULL,
    "EvaluationId" BIGINT NOT NULL UNIQUE,
    "ReceivedDate" TIMESTAMP WITH TIME ZONE NOT NULL,
    "UacrResult" NUMERIC(10,3),
    "CreatinineResult" NUMERIC(10,3),
    "ResultColor" VARCHAR(25),
    "Normality" VARCHAR(25) NOT NULL,
    "NormalityCode" VARCHAR(1) NOT NULL,
    "ResultDescription" VARCHAR(200),
    "CreatedDateTime" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

ALTER TABLE public."LabResult" OWNER TO flywayuacr;
GRANT SELECT, UPDATE, INSERT ON public."LabResult" TO svcuacr;

CREATE INDEX IDX_LabResult_EvaluationId ON public."LabResult" ("EvaluationId");

GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO svcuacr;