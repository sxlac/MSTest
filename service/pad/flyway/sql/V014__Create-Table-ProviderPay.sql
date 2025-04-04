CREATE TABLE "ProviderPay"
(
    "ProviderPayId" SERIAL PRIMARY KEY,
    "PaymentId" VARCHAR(50) NOT NULL,
    "PADId" INTEGER NOT NULL REFERENCES "PAD"("PADId"),
    "CreatedDateTime" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);