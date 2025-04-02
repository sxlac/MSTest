ALTER TABLE  public."LabResults"
ADD COLUMN IF NOT EXISTS "CreatedDateTime" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;

UPDATE  public."LabResults"
set "CreatedDateTime" = "ReleaseDate"