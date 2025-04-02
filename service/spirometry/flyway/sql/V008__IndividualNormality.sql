-- Add new columns, temporarily setting to null
ALTER TABLE public."SpirometryExamResults"
ADD COLUMN "FVCNormalityIndicatorId" SMALLINT NULL REFERENCES "NormalityIndicator" ("NormalityIndicatorId");

ALTER TABLE public."SpirometryExamResults"
ADD COLUMN "FEV1NormalityIndicatorId" SMALLINT NULL REFERENCES "NormalityIndicator" ("NormalityIndicatorId");

-- Indexes
CREATE INDEX IDX_SpirometryExamResults_FvcNormalityIndicatorId ON public."SpirometryExamResults" ("FVCNormalityIndicatorId");
CREATE INDEX IDX_SpirometryExamResults_Fev1NormalityIndicatorId ON public."SpirometryExamResults" ("FEV1NormalityIndicatorId");

-- Update existing records
UPDATE  public."SpirometryExamResults"
   SET  "FVCNormalityIndicatorId" = 1 -- Undetermined
 WHERE  "FVC" IS NULL;

UPDATE  public."SpirometryExamResults"
   SET  "FEV1NormalityIndicatorId" = 1 -- Undetermined
 WHERE  "FEV1" IS NULL;

UPDATE  public."SpirometryExamResults"
   SET  "FVCNormalityIndicatorId" = 3 -- Abnormal
 WHERE  "FVC" < 80;

UPDATE  public."SpirometryExamResults"
   SET  "FEV1NormalityIndicatorId" = 3 -- Abnormal
 WHERE  "FEV1" < 80;

UPDATE  public."SpirometryExamResults"
   SET  "FVCNormalityIndicatorId" = 2 -- Normal
 WHERE  "FVCNormalityIndicatorId" IS NULL;

UPDATE  public."SpirometryExamResults"
   SET  "FEV1NormalityIndicatorId" = 2 -- Normal
 WHERE  "FEV1NormalityIndicatorId" IS NULL;

-- Set not null constraint going forward
ALTER TABLE public."SpirometryExamResults"
ALTER COLUMN "FVCNormalityIndicatorId" SET NOT NULL;

ALTER TABLE public."SpirometryExamResults"
ALTER COLUMN "FEV1NormalityIndicatorId" SET NOT NULL;
