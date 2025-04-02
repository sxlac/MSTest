CREATE TABLE public."CdiEventForPayment"
(
    "CdiEventForPaymentId" SERIAL PRIMARY KEY NOT NULL,
    "EvaluationId" BIGINT NOT NULL,
    "RequestId" UUID NOT NULL UNIQUE,
    "Username" VARCHAR(50) NOT NULL,
    "EventType" VARCHAR(30) NOT NULL, -- Type of Cdi Event, either CDIPassedEvent or CDIFailedEvent
    "ApplicationId" VARCHAR(50) NOT NULL,
    "PayProvider" BOOLEAN,
    "Reason" VARCHAR(256),
    "DateTime" TIMESTAMP WITH TIME ZONE NOT NULL, -- DateTime contained within the Cdi Event
    "CreatedDateTime" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP -- When the Cdi Event was created within the Spiro PM
);

ALTER TABLE public."CdiEventForPayment" OWNER TO flywayspirometry;
GRANT SELECT, INSERT ON public."CdiEventForPayment" TO svcspirometry;

GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO svcspirometry;

CREATE INDEX IDX_CdiEventForPayment_EvaluationId ON public."CdiEventForPayment" ("EvaluationId");
