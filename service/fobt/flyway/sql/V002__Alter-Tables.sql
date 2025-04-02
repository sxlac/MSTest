CREATE TABLE "FOBTBarcodeHistory"
(
	"FOBTBarcodeHistoryId" SERIAL PRIMARY KEY,
    "FOBTId" INTEGER NOT NULL REFERENCES "FOBT"("FOBTId"),
    "Barcode" VARCHAR(200) NOT NULL,
	"CreatedDateTime" TIMESTAMP WITH TIME ZONE NOT NULL
);

INSERT INTO "FOBTStatusCode" ("FOBTStatusCodeId", "StatusCode") VALUES  (6, 'BarcodeUpdated');