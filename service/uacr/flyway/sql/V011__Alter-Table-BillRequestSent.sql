ALTER TABLE public."BillRequest" ADD COLUMN "Accepted" BOOLEAN NULL;
ALTER TABLE public."BillRequest" ADD COLUMN "AcceptedAt" TIMESTAMP WITH TIME ZONE NULL;

ALTER TABLE public."BillRequest" ALTER COLUMN "Accepted" SET DEFAULT FALSE;