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

ALTER TABLE public."PdfDeliveredToClient" OWNER TO flywayspirometry;
GRANT SELECT, UPDATE, INSERT ON public."PdfDeliveredToClient" TO svcspirometry;

CREATE INDEX IDX_PdfDeliveredToClient_EvaluationId ON public."PdfDeliveredToClient" ("EvaluationId");

--

CREATE TABLE public."BillRequestSent"
(
    "BillRequestSentId" SERIAL PRIMARY KEY NOT NULL,
    "SpirometryExamId" INTEGER NOT NULL REFERENCES "SpirometryExam" ("SpirometryExamId"),
    "BillId" UUID NOT NULL UNIQUE,
    "CreatedDateTime" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

ALTER TABLE public."BillRequestSent" OWNER TO flywayspirometry;
GRANT SELECT, UPDATE, INSERT ON public."BillRequestSent" TO svcspirometry;

CREATE INDEX IDX_BillRequestSent_SpirometryExamId ON public."BillRequestSent" ("SpirometryExamId");
CREATE INDEX IDX_BillRequestSent_BillId ON public."BillRequestSent" ("BillId");

--

ALTER TABLE public."SpirometryExam"
ADD COLUMN IF NOT EXISTS "ClientId" INTEGER NULL; -- Identifier of the client (ie insurance company) (comes from EvaluationFinalizedEvent); this is sent in the request to RCM to create a bill

GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO svcspirometry;
