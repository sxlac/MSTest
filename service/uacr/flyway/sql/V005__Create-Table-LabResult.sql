CREATE TABLE public."LabResult"
(
    "LabResultId" SERIAL PRIMARY KEY NOT NULL,
    "CenseoId" VARCHAR(8) COLLATE pg_catalog."default",
    "Barcode" VARCHAR(200) UNIQUE NOT NULL,
    "CollectionDate" TIMESTAMP WITH TIME ZONE,
    "AccessionedDate" TIMESTAMP WITH TIME ZONE,
    "CreatedDateTime" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

ALTER TABLE public."LabResult" OWNER TO flywayuacr;
GRANT SELECT, UPDATE, INSERT ON public."LabResult" TO svcuacr;

CREATE INDEX IDX_LabResult_CenseoId ON public."LabResult" ("CenseoId");

GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO svcuacr;