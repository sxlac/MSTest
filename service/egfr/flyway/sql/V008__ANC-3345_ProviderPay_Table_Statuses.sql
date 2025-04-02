CREATE TABLE public."ProviderPay"
(
	"ProviderPayId" SERIAL PRIMARY KEY,
	"PaymentId" VARCHAR(50) NOT NULL UNIQUE,
	"ExamId" INTEGER NOT NULL UNIQUE REFERENCES "Exam"("ExamId"),
    "CreatedDateTime" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

ALTER TABLE public."ProviderPay" OWNER TO flywayegfr;
GRANT SELECT, INSERT ON public."ProviderPay" TO svcegfr;

GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO svcegfr;

INSERT INTO "ExamStatusCode"
    ("ExamStatusCodeId", "StatusName")
VALUES
    (8, 'ProviderPayableEventReceived'),
    (9, 'ProviderPayRequestSent'),
    (10, 'ProviderNonPayableEventReceived'),
    (11, 'CDIPassedReceived'),
    (12, 'CDIFailedWithPayReceived'),
    (13, 'CDIFailedWithoutPayReceived');