CREATE TABLE public."ProviderPay"
(
    "ProviderPayId" SERIAL PRIMARY KEY,
    "PaymentId" VARCHAR(50) NOT NULL UNIQUE,
    "ExamId" INTEGER NOT NULL UNIQUE REFERENCES "Exam"("ExamId"),
    "CreatedDateTime" TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

ALTER TABLE public."ProviderPay" OWNER TO flywayuacr;
GRANT SELECT, INSERT ON public."ProviderPay" TO svcuacr;

CREATE INDEX IDX_ProviderPay_ExamId ON public."ProviderPay" ("ExamId");

GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO svcuacr;