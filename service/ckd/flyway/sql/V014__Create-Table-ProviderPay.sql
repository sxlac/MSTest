CREATE TABLE "ProviderPay"
(
    "ProviderPayId" SERIAL PRIMARY KEY,
    "PaymentId" VARCHAR(50) NOT NULL,
    "CKDId" INTEGER NOT NULL REFERENCES "CKD"("CKDId"),
    "CreatedDateTime" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);