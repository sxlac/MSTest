CREATE TABLE IF NOT EXISTS "LabResults"
(
    "LabResultId" SERIAL PRIMARY KEY,
    "A1CId" INTEGER NOT NULL REFERENCES "A1C"("A1CId"),
    "OrderCorrelationId" UUID NOT NULL,
    "ReceivedDateTime" TIMESTAMP WITH TIME ZONE NULL,
    "Barcode" VARCHAR(200) NULL,
    "LabResult" VARCHAR(4000) NULL,
    "ProductCode" VARCHAR(50) NULL,
    "AbnormalIndicator" VARCHAR(100) NULL,
    "Exception" VARCHAR(255) NULL,
    "CollectionDate" TIMESTAMP WITH TIME ZONE NULL,
    "ServiceDate" TIMESTAMP WITH TIME ZONE NULL,
    "ReleaseDate" TIMESTAMP WITH TIME ZONE NULL
);

GRANT ALL ON DATABASE A1C TO a1csvc WITH GRANT OPTION;

UPDATE "A1CStatusCode" SET "StatusCode"='ValidLabResultsReceived' WHERE "A1CStatusCodeId"= 5;
INSERT INTO "A1CStatusCode" ("A1CStatusCodeId", "StatusCode")  VALUES  (9,'InvalidLabResultsReceived') ON CONFLICT DO NOTHING;
