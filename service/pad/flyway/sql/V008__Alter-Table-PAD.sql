ALTER TABLE "PAD"
ADD COLUMN IF NOT EXISTS "LeftNormalityIndicator" VARCHAR(10) NULL,
ADD COLUMN IF NOT EXISTS "RightNormalityIndicator" VARCHAR(10) NULL,
ADD COLUMN IF NOT EXISTS "ProcessDateTime" TIMESTAMP WITH TIME ZONE NOT NULL default CURRENT_DATE;
