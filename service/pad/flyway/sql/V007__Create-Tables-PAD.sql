CREATE TABLE IF NOT EXISTS "SeverityLookup"
(
    "SeverityLookupId" SERIAL PRIMARY KEY,
    "MinScore" NUMERIC(5,2) NULL,
    "MaxScore" NUMERIC(5,2) NULL,
    "Severity" VARCHAR(50) NOT NULL,
    "NormalityIndicator" VARCHAR(10) NOT NULL   
);

CREATE TABLE IF NOT EXISTS  "NotPerformed"
(
    "NotPerformedId" SERIAL PRIMARY KEY,
    "PADId" INTEGER NOT NULL REFERENCES "PAD" ("PADId"),
    "AnswerId" INTEGER NULL
);

GRANT ALL ON DATABASE pad TO padsvc WITH GRANT OPTION;


INSERT INTO "SeverityLookup"("MaxScore","MinScore","Severity","NormalityIndicator")
Values(1.40,1.00,'Normal','N'),
(0.99,0.90,'Borderline','N'),
(0.89,0.60,'Mild','A'),
(0.59,0.30,'Moderate','A'),
(0.29,0.00,'Severe','A')
 ON CONFLICT ("SeverityLookupId") DO NOTHING;