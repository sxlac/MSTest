CREATE TABLE public."LabResult"
(
    "LabResultId" SERIAL PRIMARY KEY NOT NULL,
	"CenseoId" character varying(8) COLLATE pg_catalog."default",
	"VendorLabTestId" bigint,
	"VendorLabTestNumber" character varying(25) COLLATE pg_catalog."default",
	"eGFRResult" integer,
	"CreatinineResult" numeric(3,2),
	"Normality" character varying(25) COLLATE pg_catalog."default",
	"NormalityCode" character varying(1) COLLATE pg_catalog."default",
	"MailDate" timestamp with time zone,
	"CollectionDate" timestamp with time zone,
	"AccessionedDate" timestamp with time zone,
	"CreatedDateTime" timestamp with time zone NOT NULL DEFAULT NOW()
);

ALTER TABLE public."LabResult" OWNER TO flywayegfr;
GRANT SELECT, UPDATE, INSERT ON public."LabResult" TO svcegfr;

CREATE INDEX IDX_LabResult_CenseoId ON public."LabResult" ("CenseoId");

GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO svcegfr;

