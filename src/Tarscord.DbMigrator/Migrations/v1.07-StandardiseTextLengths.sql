-- 200 and 1000 were arbitrary. Free text is capped at what Discord lets someone type in one message,
-- and a name at what an embed title can show, so the only input the bot refuses is input Discord
-- could not have sent it in the first place.
ALTER TABLE public.event_infos
    ALTER COLUMN event_name TYPE VARCHAR(256),
    ALTER COLUMN event_description TYPE VARCHAR(2000);

ALTER TABLE public.loans
    ALTER COLUMN description TYPE VARCHAR(2000);

ALTER TABLE public.reminders
    ALTER COLUMN message TYPE VARCHAR(2000);
