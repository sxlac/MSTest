ALTER TABLE public."BillRequestSent" ADD COLUMN "Accepted" BOOLEAN NULL;
ALTER TABLE public."BillRequestSent" ADD COLUMN "AcceptedAt" TIMESTAMP WITH TIME ZONE NULL;

ALTER TABLE public."BillRequestSent" ALTER COLUMN "Accepted" SET DEFAULT FALSE;

