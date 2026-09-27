-- The C# model and these tables disagreed on the type of almost every column. Nothing failed
-- loudly, so the damage showed up as wrong answers: money lost its cents, event organizers could
-- not be written at all, and event_attendees was unusable.

-- Money was BIGINT behind a decimal property, so 12.50 was rounded to 13 on the way in and a
-- partial-payment balance could never settle.
ALTER TABLE public.loans
    ALTER COLUMN amount_loaned TYPE NUMERIC(18, 2),
    ALTER COLUMN amount_payed TYPE NUMERIC(18, 2);

UPDATE public.loans SET amount_payed = 0 WHERE amount_payed IS NULL;
UPDATE public.loans SET loaned_from_id = 0 WHERE loaned_from_id IS NULL;
UPDATE public.loans SET loaned_to_id = 0 WHERE loaned_to_id IS NULL;
UPDATE public.loans SET loaned_from = '' WHERE loaned_from IS NULL;
UPDATE public.loans SET loaned_to = '' WHERE loaned_to IS NULL;
UPDATE public.loans SET description = '' WHERE description IS NULL;

ALTER TABLE public.loans
    ALTER COLUMN amount_payed SET NOT NULL,
    ALTER COLUMN amount_payed SET DEFAULT 0,
    ALTER COLUMN loaned_from_id SET NOT NULL,
    ALTER COLUMN loaned_to_id SET NOT NULL,
    ALTER COLUMN loaned_from SET NOT NULL,
    ALTER COLUMN loaned_to SET NOT NULL,
    ALTER COLUMN description SET NOT NULL,
    ALTER COLUMN created TYPE TIMESTAMPTZ,
    ALTER COLUMN updated TYPE TIMESTAMPTZ;

-- Loans are always looked up by the pair of user ids.
CREATE INDEX IF NOT EXISTS ix_loans_loaned_from_id ON public.loans (loaned_from_id);
CREATE INDEX IF NOT EXISTS ix_loans_loaned_to_id ON public.loans (loaned_to_id);

-- event_organizer_id held a BIGINT behind a string property, so EF sent a text parameter to a
-- bigint column and PostgreSQL refused the cast: creating an event could not succeed.
UPDATE public.event_infos SET event_organizer_id = 0 WHERE event_organizer_id IS NULL;
UPDATE public.event_infos SET event_organizer = '' WHERE event_organizer IS NULL;
UPDATE public.event_infos SET event_description = '' WHERE event_description IS NULL;
UPDATE public.event_infos SET is_active = TRUE WHERE is_active IS NULL;

ALTER TABLE public.event_infos
    ALTER COLUMN event_organizer_id SET NOT NULL,
    ALTER COLUMN event_organizer SET NOT NULL,
    ALTER COLUMN event_description SET NOT NULL,
    ALTER COLUMN is_active SET NOT NULL,
    ALTER COLUMN is_active SET DEFAULT TRUE,
    ALTER COLUMN event_date TYPE TIMESTAMPTZ,
    ALTER COLUMN created TYPE TIMESTAMPTZ,
    ALTER COLUMN updated TYPE TIMESTAMPTZ;

-- attendee_id is a Discord snowflake, which overflows INTEGER outright.
UPDATE public.event_attendees SET attendee_id = 0 WHERE attendee_id IS NULL;
UPDATE public.event_attendees SET attendee_name = '' WHERE attendee_name IS NULL;
UPDATE public.event_attendees SET confirmed = FALSE WHERE confirmed IS NULL;
DELETE FROM public.event_attendees WHERE event_info_id IS NULL;

ALTER TABLE public.event_attendees
    ALTER COLUMN attendee_id TYPE BIGINT,
    ALTER COLUMN attendee_id SET NOT NULL,
    ALTER COLUMN attendee_name SET NOT NULL,
    ALTER COLUMN confirmed SET NOT NULL,
    ALTER COLUMN confirmed SET DEFAULT FALSE,
    ALTER COLUMN event_info_id SET NOT NULL,
    ALTER COLUMN created TYPE TIMESTAMPTZ,
    ALTER COLUMN updated TYPE TIMESTAMPTZ;

-- One attendance row per person per event, so confirming twice updates rather than duplicates.
DELETE FROM public.event_attendees a
WHERE a.id > (SELECT MIN(b.id)
              FROM public.event_attendees b
              WHERE b.event_info_id = a.event_info_id
                AND b.attendee_id = a.attendee_id);

CREATE UNIQUE INDEX IF NOT EXISTS ux_event_attendees_event_attendee
    ON public.event_attendees (event_info_id, attendee_id);

-- Entities/User.cs has existed with mute fields since the mute command was written and never had a
-- table behind it, so a mute was applied to Discord and forgotten immediately.
CREATE TABLE IF NOT EXISTS public.users
(
    id                  SERIAL PRIMARY KEY,
    discord_id          BIGINT       NOT NULL,
    username            VARCHAR(100) NOT NULL,
    is_muted            BOOLEAN      NOT NULL DEFAULT FALSE,
    muted_until         TIMESTAMPTZ           DEFAULT NULL,
    can_not_react       BOOLEAN      NOT NULL DEFAULT FALSE,
    can_not_react_until TIMESTAMPTZ           DEFAULT NULL,
    created             TIMESTAMPTZ  NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated             TIMESTAMPTZ           DEFAULT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_users_discord_id ON public.users (discord_id);
