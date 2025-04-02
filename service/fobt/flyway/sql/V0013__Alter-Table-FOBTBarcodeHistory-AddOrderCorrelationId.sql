ALTER TABLE public."FOBTBarcodeHistory" ADD COLUMN IF NOT EXISTS "OrderCorrelationId" UUID NULL;

UPDATE public."FOBTStatusCode"
SET  "StatusCode" = 'OrderUpdated'
WHERE "FOBTStatusCodeId" = 6;