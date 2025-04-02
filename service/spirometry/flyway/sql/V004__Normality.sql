CREATE TABLE public."NormalityIndicator"
(
    "NormalityIndicatorId" SMALLSERIAL PRIMARY KEY NOT NULL,
    "Normality" VARCHAR(128) UNIQUE NOT NULL,
    "Indicator" CHAR(1) UNIQUE NOT NULL
);

ALTER TABLE public."NormalityIndicator" OWNER TO flywayspirometry;
GRANT SELECT ON public."NormalityIndicator" TO svcspirometry;

INSERT INTO public."NormalityIndicator"
        ("Normality", "Indicator")
VALUES  ('Undetermined', 'U'),
        ('Normal', 'N'),
        ('Abnormal', 'A');

CREATE INDEX IDX_NormalityIndicator_Indicator ON public."NormalityIndicator" ("Indicator");

ALTER TABLE public."SpirometryExamResults"
ADD COLUMN IF NOT EXISTS "NormalityIndicatorId" SMALLINT NULL REFERENCES "NormalityIndicator" ("NormalityIndicatorId"); -- Temporarily default to null until update below is run

UPDATE  public."SpirometryExamResults" r
   SET  "NormalityIndicatorId" = CASE
            WHEN s."IsGradable" = true AND r."FEV1_Over_FVC" >= 0.7 THEN (SELECT "NormalityIndicatorId" FROM public."NormalityIndicator" n WHERE n."Normality" = 'Normal')
            WHEN s."SessionGradeId" IS NOT NULL AND r."FEV1_Over_FVC" IS NOT NULL THEN (SELECT "NormalityIndicatorId" FROM public."NormalityIndicator" n WHERE n."Normality" = 'Abnormal')
            ELSE (SELECT "NormalityIndicatorId" FROM public."NormalityIndicator" n WHERE n."Normality" = 'Undetermined')
        END
  FROM  public."SessionGrade" s
 WHERE  r."SessionGradeId" = s."SessionGradeId";
 
UPDATE  public."SpirometryExamResults"
   SET  "NormalityIndicatorId" = (SELECT "NormalityIndicatorId" FROM public."NormalityIndicator" n WHERE n."Normality" = 'Undetermined')
 WHERE  "SessionGradeId" IS NULL;

ALTER TABLE public."SpirometryExamResults"
ALTER COLUMN "NormalityIndicatorId" SET NOT NULL;

CREATE INDEX IDX_SpirometryExamResults_NormalityIndicatorId ON public."SpirometryExamResults" ("NormalityIndicatorId");
