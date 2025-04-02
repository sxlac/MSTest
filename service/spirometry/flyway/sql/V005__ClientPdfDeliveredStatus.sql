INSERT INTO public."StatusCode"
        ("Name")
SELECT  'Client PDF Delivered'
 WHERE	NOT EXISTS (SELECT * FROM public."StatusCode" WHERE "Name" = 'Client PDF Delivered');

INSERT	INTO public."ExamStatus"
		("SpirometryExamId", "StatusCodeId", "StatusDateTime")
SELECT	r."SpirometryExamId", 1 /*Performed*/, r."CreatedDateTime"
  FROM	public."SpirometryExamResults" r
 WHERE	NOT EXISTS (SELECT * FROM public."ExamStatus" WHERE "StatusCodeId" = 1)
 ORDER	BY r."SpirometryExamId" ASC;

INSERT	INTO public."ExamStatus"
		("SpirometryExamId", "StatusCodeId", "StatusDateTime")
SELECT	n."SpirometryExamId", 2 /*Not Performed*/, n."CreatedDateTime"
  FROM	public."ExamNotPerformed" n
 WHERE	NOT EXISTS (SELECT * FROM "ExamStatus" WHERE "StatusCodeId" = 2)
 ORDER	BY n."SpirometryExamId" ASC;

INSERT	INTO public."ExamStatus"
		("SpirometryExamId", "StatusCodeId", "StatusDateTime")
SELECT	e."SpirometryExamId", 5 /*PDF Delivered*/, p."CreatedDateTime"
  FROM	public."PdfDeliveredToClient" p
 INNER	JOIN public."SpirometryExam" e ON (e."EvaluationId" = p."EvaluationId")
 WHERE	NOT EXISTS (SELECT * FROM "ExamStatus" WHERE "StatusCodeId" = 5)
 ORDER	BY p."CreatedDateTime" ASC;

INSERT	INTO public."ExamStatus"
		("SpirometryExamId", "StatusCodeId", "StatusDateTime")
SELECT	s."SpirometryExamId", 3 /*Billable event received*/, MIN(s."StatusDateTime") -- First time a Client PDF Delivered event occurred for the exam, in case there were multiple. Only the first one is a billable event.
  FROM	public."ExamStatus" s
 INNER	JOIN public."StatusCode" sc ON (sc."StatusCodeId" = s."StatusCodeId")
 INNER	JOIN public."SpirometryExamResults" r ON (r."SpirometryExamId" = s."SpirometryExamId")
 INNER	JOIN public."NormalityIndicator" n ON (n."NormalityIndicatorId" = r."NormalityIndicatorId")
 WHERE	sc."Name" = 'Client PDF Delivered'
   AND	n."Normality" IN ('Normal','Abnormal')
   AND	NOT EXISTS (SELECT * FROM "ExamStatus" WHERE "StatusCodeId" = 3)
 GROUP  BY s."SpirometryExamId" -- Just in case there are multiple
 ORDER	BY s."SpirometryExamId" ASC;

INSERT	INTO public."ExamStatus"
		("SpirometryExamId", "StatusCodeId", "StatusDateTime")
SELECT	b."SpirometryExamId", 4 /*Bill request sent*/, b."CreatedDateTime"
  FROM	public."BillRequestSent" b
 WHERE	NOT EXISTS (SELECT * FROM "ExamStatus" WHERE "StatusCodeId" = 4)
 ORDER	BY b."CreatedDateTime" ASC;

ALTER TABLE public."ExamStatus" ALTER COLUMN "CreateDateTime" SET NOT NULL;
ALTER TABLE public."ExamStatus" ALTER COLUMN "CreateDateTime" SET DEFAULT NOW();
ALTER TABLE public."SpirometryExam" ALTER COLUMN "EvaluationId" SET NOT NULL;
ALTER TABLE public."SpirometryExam" ALTER COLUMN "ProviderId" SET NOT NULL;
ALTER TABLE public."SpirometryExam" ALTER COLUMN "MemberId" SET NOT NULL;
ALTER TABLE public."SpirometryExam" ALTER COLUMN "MemberPlanId" SET NOT NULL;
ALTER TABLE public."SpirometryExam" ALTER COLUMN "AppointmentId" SET NOT NULL;
