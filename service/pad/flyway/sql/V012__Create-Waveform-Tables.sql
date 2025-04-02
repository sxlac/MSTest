CREATE TABLE IF NOT EXISTS  "WaveformDocumentVendor"
(
    "WaveformDocumentVendorId" SERIAL PRIMARY KEY,
    "VendorName" VARCHAR(256) NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS "WaveformDocument"
(
    "WaveformDocumentId" SERIAL PRIMARY KEY,
    "WaveformDocumentVendorId" INTEGER NOT NULL REFERENCES "WaveformDocumentVendor" ("WaveformDocumentVendorId"),
    "Filename" VARCHAR(256) NOT NULL,
    "MemberPlanId" BIGINT NOT NULL,
    "DateOfExam" DATE NOT NULL,
    "CreatedDateTime" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS IDX_WaveformDocument_WaveformDocumentVendorId ON public."WaveformDocument" ("WaveformDocumentVendorId");

INSERT INTO "PADStatusCode" ("PADStatusCodeId", "StatusCode") VALUES  (5, 'WaveformDocumentDownloaded') ON CONFLICT DO NOTHING;
INSERT INTO "PADStatusCode" ("PADStatusCodeId", "StatusCode") VALUES  (6, 'WaveformDocumentUploaded') ON CONFLICT DO NOTHING;

INSERT INTO "WaveformDocumentVendor" ("VendorName") VALUES  ('Semler Scientific') ON CONFLICT DO NOTHING;