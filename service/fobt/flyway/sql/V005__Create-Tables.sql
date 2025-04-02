
CREATE TABLE "LabResults"
(
    "LabResultId" SERIAL PRIMARY KEY,
    "OrderCorrelationId" UUID NOT NULL,
	"FOBTId" INTEGER NOT NULL REFERENCES "FOBT"("FOBTId"),
	"Barcode" VARCHAR(20) NOT NULL,
	"LabResult" VARCHAR(4000) NOT NULL,
	"ProductCode" VARCHAR(50) NOT NULL,
	"AbnormalIndicator" VARCHAR(100) NOT NULL,
	"Exception" VARCHAR(255) NOT NULL,
    "CollectionDate" TIMESTAMP WITH TIME ZONE NULL,
    "ServiceDate" TIMESTAMP WITH TIME ZONE NULL,
	"ReleaseDate" TIMESTAMP WITH TIME ZONE NULL
);


INSERT INTO "FOBTStatusCode" ("FOBTStatusCodeId", "StatusCode")  VALUES  (7,'ValidLabResultsReceived') ON CONFLICT DO NOTHING;
INSERT INTO "FOBTStatusCode" ("FOBTStatusCodeId", "StatusCode")  VALUES  (10,'InvalidLabResultsReceived') ON CONFLICT DO NOTHING;




