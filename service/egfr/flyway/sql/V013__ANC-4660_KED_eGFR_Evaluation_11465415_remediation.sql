UPDATE public."BarcodeHistory"
SET "Barcode" = 'LGC-4894-2581-6458|BSWAGO'
WHERE "Barcode" LIKE 'LGC-4894-2581-6458%'
AND "Barcode" NOT LIKE '%|BSWAGO';