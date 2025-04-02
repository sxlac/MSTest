INSERT INTO public."StatusCode"
        ("Name")
SELECT  'Bill Request Not Sent'
 WHERE	NOT EXISTS (SELECT * FROM public."StatusCode" WHERE "Name" = 'Bill Request Not Sent');