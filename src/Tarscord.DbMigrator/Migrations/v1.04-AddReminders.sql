-- Held in a static SortedList before this: lost on restart, and two in one tick collided.
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
