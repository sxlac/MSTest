CREATE TABLE public."ProviderPay"
(
    "ProviderPayId" SERIAL PRIMARY KEY,
    "SpirometryExamId" INTEGER NOT NULL UNIQUE REFERENCES "SpirometryExam"("SpirometryExamId"),
    "PaymentId" VARCHAR(50) NOT NULL UNIQUE,
    "CreatedDateTime" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

ALTER TABLE public."ProviderPay" OWNER TO flywayspirometry;
GRANT SELECT, INSERT ON public."ProviderPay" TO svcspirometry;

GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO svcspirometry;

INSERT INTO public."StatusCode"
    ("Name")
VALUES
    ('Provider Payable Event Received'),
    ('Provider Pay Request Sent'),
    ('Provider Non-Payable Event Received'),
    ('CDI Passed Received'),
    ('CDI Failed with Pay Received'),
    ('CDI Failed without Pay Received');
