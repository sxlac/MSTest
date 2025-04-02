CREATE TABLE public."Hold"
(
	"HoldId" SERIAL PRIMARY KEY NOT NULL,
	"CdiHoldId" UUID NOT NULL UNIQUE,
	"EvaluationId" INTEGER NOT NULL UNIQUE,
	"ExpiresAt" TIMESTAMP WITH TIME ZONE NOT NULL,
	"HeldOnDateTime" TIMESTAMP WITH TIME ZONE NOT NULL,
	"SentAtDateTime" TIMESTAMP WITH TIME ZONE NOT NULL,
	"ReleasedDateTime" TIMESTAMP WITH TIME ZONE NULL,
	"CreatedDateTime" TIMESTAMP WITH TIME ZONE NOT NULL
);

ALTER TABLE public."Hold" OWNER TO flywayspirometry;
GRANT SELECT, UPDATE, INSERT ON public."Hold" TO svcspirometry;

CREATE INDEX IDX_Hold_EvaluationId ON public."Hold" ("EvaluationId");

--

CREATE TABLE public."OverreadResult"
(
    "OverreadResultId" SERIAL PRIMARY KEY NOT NULL,
    "ExternalId" UUID NOT NULL,
    "AppointmentId" BIGINT NOT NULL UNIQUE,
    "SessionId" UUID NOT NULL,
    "Fev1FvcRatio" NUMERIC(3,2) NOT NULL,
    "NormalityIndicatorId" SMALLINT NOT NULL REFERENCES "NormalityIndicator" ("NormalityIndicatorId"),
    "PerformedDateTime" TIMESTAMP WITH TIME ZONE NOT NULL,
    "OverreadDateTime" TIMESTAMP WITH TIME ZONE NOT NULL,
    "OverreadBy" VARCHAR NOT NULL,
    "OverreadComment" VARCHAR NOT NULL,
    "BestTestId" UUID NULL,
    "BestFvcTestComment" VARCHAR NOT NULL,
    "BestFvcTestId" UUID NULL,
    "BestFev1TestComment" VARCHAR NOT NULL,
    "BestFev1TestId" UUID NULL,
    "BestPefTestComment" VARCHAR NOT NULL,
    "BestPefTestId" UUID NULL,
    "ReceivedDateTime" TIMESTAMP WITH TIME ZONE NOT NULL, -- Time received by the webhook
    "CreatedDateTime" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

ALTER TABLE public."OverreadResult" OWNER TO flywayspirometry;
GRANT SELECT, UPDATE, INSERT ON public."OverreadResult" TO svcspirometry;

CREATE INDEX IDX_OverreadResult_NormalityIndicatorId ON public."OverreadResult" ("NormalityIndicatorId");

--

ALTER TABLE public."SpirometryExam" ALTER COLUMN "AppointmentId" TYPE BIGINT;

ALTER TABLE public."SpirometryExam" ADD CONSTRAINT "SpirometryExam_AppointmentId_key" UNIQUE ("AppointmentId");

GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO svcspirometry;
