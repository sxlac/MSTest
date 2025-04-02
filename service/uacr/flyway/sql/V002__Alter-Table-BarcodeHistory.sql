
ALTER TABLE public."BarcodeHistory" RENAME TO "BarcodeExam";
ALTER TABLE public."BarcodeExam" RENAME COLUMN "BarcodeHistoryId" TO "BarcodeExamId";
ALTER TABLE public."BarcodeExam" OWNER TO flywayuacr;
GRANT SELECT, INSERT ON public."BarcodeExam" TO svcuacr;

CREATE INDEX IDX_BarcodeExam_ExamId ON public."BarcodeExam" ("ExamId");