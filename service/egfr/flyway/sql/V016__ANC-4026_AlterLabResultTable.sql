CREATE TABLE public."NormalityIndicator"
(
    "NormalityIndicatorId" SERIAL PRIMARY KEY NOT NULL,
    "Normality" VARCHAR(128) UNIQUE NOT NULL,
    "Indicator" CHAR(1) UNIQUE NOT NULL
);

ALTER TABLE public."NormalityIndicator" OWNER TO flywayegfr;
GRANT SELECT ON public."NormalityIndicator" TO svcegfr;

INSERT INTO public."NormalityIndicator"
("Normality", "Indicator")
VALUES  ('Undetermined', 'U'),
        ('Normal', 'N'),
        ('Abnormal', 'A');

ALTER TABLE IF EXISTS "LabResult"
    RENAME TO "QuestLabResult";
ALTER SEQUENCE "LabResult_LabResultId_seq" RENAME TO "QuestLabResult_LabResultId_seq";
ALTER INDEX IDX_LabResult_CenseoId RENAME TO "idx_questlabresult_censeoid";
ALTER INDEX "LabResult_pkey" RENAME TO "QuestLabResult_pkey";

ALTER TABLE public."QuestLabResult" OWNER TO flywayegfr;
GRANT SELECT, UPDATE, INSERT ON public."QuestLabResult" TO svcegfr;

CREATE TABLE public."LabResult"
(
    "LabResultId" SERIAL PRIMARY KEY NOT NULL,
    "ExamId" INTEGER NOT NULL UNIQUE REFERENCES "Exam" ("ExamId"),
    "ReceivedDate" TIMESTAMP WITH TIME ZONE NOT NULL,
    "EgfrResult" INTEGER,
    "NormalityIndicatorId" INTEGER NOT NULL REFERENCES "NormalityIndicator" ("NormalityIndicatorId"),
    "ResultDescription" VARCHAR(200),
    "CreatedDateTime" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

ALTER TABLE public."LabResult" OWNER TO flywayegfr;
GRANT SELECT, UPDATE, INSERT ON public."LabResult" TO svcegfr;

CREATE INDEX IDX_LabResult_ExamId ON public."LabResult" ("ExamId");
CREATE INDEX IDX_LabResult_NormalityIndicatorId ON public."LabResult" ("NormalityIndicatorId");

GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO svcegfr;