DROP TABLE IF EXISTS public."LabResult";
DROP TABLE IF EXISTS public."NormalityIndicator";

ALTER TABLE IF EXISTS "QuestLabResult"
    RENAME TO "LabResult";
ALTER SEQUENCE "QuestLabResult_LabResultId_seq" RENAME TO "LabResult_LabResultId_seq";
ALTER INDEX "idx_questlabresult_censeoid" RENAME TO "idx_labresult_censeoid";
ALTER INDEX "QuestLabResult_pkey" RENAME TO "LabResult_pkey";

ALTER TABLE public."LabResult" OWNER TO flywayegfr;
GRANT SELECT, UPDATE, INSERT ON public."LabResult" TO svcegfr;