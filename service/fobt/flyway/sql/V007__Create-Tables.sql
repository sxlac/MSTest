CREATE TABLE "PDFToClient"
(
    "PDFDeliverId" SERIAL PRIMARY KEY,
    "EventId" VARCHAR(40),
    "EvaluationId" BIGINT NOT NULL,
    "DeliveryDateTime" TIMESTAMP WITH TIME ZONE,
    "DeliveryCreatedDateTime" TIMESTAMP WITH TIME ZONE,
    "BatchId" BIGINT NOT NULL,
    "BatchName" VARCHAR(200),
    "FOBTId" INTEGER NOT NULL REFERENCES "FOBT"("FOBTId"),
    "CreatedDateTime" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
); 



CREATE TABLE "FOBTBilling"
(
    "Id" SERIAL PRIMARY KEY,
    "BillId" VARCHAR(50),
	"BillingProductCode" VARCHAR(50),
    "FOBTId" INTEGER NOT NULL REFERENCES "FOBT"("FOBTId"),
    "CreatedDateTime" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);




INSERT INTO "FOBTStatusCode" ("FOBTStatusCodeId", "StatusCode")  VALUES  (11,'ClientPDFDelivered') ON CONFLICT DO NOTHING;
INSERT INTO "FOBTStatusCode" ("FOBTStatusCodeId", "StatusCode")  VALUES  (12,'LeftBehindBillRequestSent') ON CONFLICT DO NOTHING;
INSERT INTO "FOBTStatusCode" ("FOBTStatusCodeId", "StatusCode")  VALUES  (13,'ResultsBillRequestSent') ON CONFLICT DO NOTHING;