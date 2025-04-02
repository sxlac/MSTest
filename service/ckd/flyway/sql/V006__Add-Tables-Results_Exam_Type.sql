/*
--Anti-StarTrek Script, i.e. we need to reverse.
DROP INDEX IF EXISTS IDX_ExamResult_ExamResultCKDId;

DROP TABLE IF EXISTS "ExamResult";
*/


CREATE TABLE IF NOT EXISTS "ExamResult"
(
    "ExamResultId" SERIAL PRIMARY KEY,
    "CKDId" INTEGER NOT NULL REFERENCES "CKD"("CKDId"),
	"CKDAnswer" character varying(200) COLLATE pg_catalog."default",
    "Albumin" NUMERIC NOT NULL,
    "Creatinine" NUMERIC NOT NULL,
    "Acr" CHARACTER VARYING(200) COLLATE pg_catalog."default" NOT NULL
);

CREATE INDEX IF NOT EXISTS IDX_ExamResult_ExamResultCKDId ON public."ExamResult" ("ExamResultId", "CKDId");