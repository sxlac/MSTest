ALTER TABLE public."ProviderPay"
ADD COLUMN "ProviderPayProductCode" VARCHAR(32) NOT NULL DEFAULT 'DEE';

ALTER TABLE public."ProviderPay"
ALTER COLUMN "ProviderPayProductCode" DROP DEFAULT;