-- Reminders were held in a static SortedList inside a singleton service, so they were lost on every
-- restart and two reminders due in the same tick collided on the key.
CREATE TABLE IF NOT EXISTS public.reminders
(
    id         SERIAL PRIMARY KEY,
    user_id    BIGINT       NOT NULL,
    channel_id BIGINT       NOT NULL,
    username   VARCHAR(100) NOT NULL,
    message    VARCHAR(1000) NOT NULL,
    remind_at  TIMESTAMPTZ  NOT NULL,
    sent       BOOLEAN      NOT NULL DEFAULT FALSE,
    created    TIMESTAMPTZ  NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated    TIMESTAMPTZ           DEFAULT NULL
);

-- The dispatcher asks only one question: what is due and not yet sent.
CREATE INDEX IF NOT EXISTS ix_reminders_pending ON public.reminders (sent, remind_at);
