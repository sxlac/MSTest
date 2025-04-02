ALTER TABLE "BillRequest" RENAME TO "BillRequestSent";
ALTER TABLE "BillRequestSent" RENAME COLUMN "BillRequestId" TO "BillRequestSentId";
ALTER TABLE "BillRequestSent" RENAME CONSTRAINT "BillRequest_BillId_key" TO "BillRequestSent_BillId_key";
ALTER TABLE "BillRequestSent" RENAME CONSTRAINT "BillRequest_ExamId_fkey" TO "BillRequestSent_ExamId_fkey";
ALTER TABLE "BillRequestSent" RENAME CONSTRAINT "BillRequest_pkey" TO "BillRequestSent_pkey";
ALTER TABLE idx_billrequest_examid RENAME TO idx_billrequestsent_examid;

CREATE INDEX idx_billrequestsent_billid ON public."BillRequestSent" ("BillId");

CREATE TABLE public."PdfDeliveredToClient"
(
    "PdfDeliveredToClientId" INT PRIMARY KEY NOT NULL,
    "EventId" uuid NOT NULL,
    "EvaluationId" bigint NOT NULL,
    "BatchId" bigint NOT NULL,
    "BatchName" character varying(256) COLLATE pg_catalog."default",
    "DeliveryDateTime" timestamp with time zone NOT NULL,
    "CreatedDateTime" timestamp with time zone NOT NULL
);

ALTER TABLE public."PdfDeliveredToClient" OWNER to flywayegfr;
GRANT SELECT, UPDATE, INSERT ON public."PdfDeliveredToClient" TO svcegfr;

CREATE INDEX idx_pdfdeliveredtoclient_evaluationid ON public."PdfDeliveredToClient" ("EvaluationId");

GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO svcegfr;