-- Change PAD.ReceivedDateTime from DATE to timestamp with tz
ALTER TABLE "PAD"
ALTER COLUMN "ReceivedDateTime" TYPE TIMESTAMP WITH TIME ZONE;
