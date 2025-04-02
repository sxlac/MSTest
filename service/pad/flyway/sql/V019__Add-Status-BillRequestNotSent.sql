-- No unique constraint exists yet, add one now to ensure duplicates cannot be inserted
ALTER TABLE "PADStatusCode" ADD CONSTRAINT "PADStatusCode_StatusCode_key" UNIQUE ("StatusCode");

-- Previous flyway scripts explicitly set the PK instead of using the sequence,
-- so the serial sequence is currently set to return 1 next. Restart the sequence
-- so the next identifier returned is the next one that's expected.

-- We really should make sure our inserts across all PM's going forward don't
-- explicitly set the PK identifier, but use the given auto-increment sequence instead.
ALTER SEQUENCE "PADStatusCode_PADStatusCodeId_seq" RESTART WITH 13; -- Set next value to be 13

INSERT  INTO "PADStatusCode"
        ("StatusCode")
VALUES  ('BillRequestNotSent');
