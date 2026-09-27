-- A mute is scoped to a channel, so per-user columns cannot express it: muting someone in two
-- channels needs two expiries, and users.is_muted could only ever remember one of them. v1.03 added
-- that table on the strength of Entities/User.cs, which no code ever read or wrote. This replaces it
-- with a row per restriction.
DROP TABLE IF EXISTS public.users;

CREATE TABLE IF NOT EXISTS public.restrictions
(
    id         SERIAL PRIMARY KEY,
    user_id    BIGINT       NOT NULL,
    username   VARCHAR(100) NOT NULL,
    channel_id BIGINT       NOT NULL,
    kind       VARCHAR(20)  NOT NULL,
    expires_at TIMESTAMPTZ           DEFAULT NULL,
    lifted     BOOLEAN      NOT NULL DEFAULT FALSE,
    created    TIMESTAMPTZ  NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated    TIMESTAMPTZ           DEFAULT NULL
);

-- The sweeper asks only what has expired and is still in force.
CREATE INDEX IF NOT EXISTS ix_restrictions_pending ON public.restrictions (lifted, expires_at);

-- One restriction of a kind in force per user per channel.
CREATE UNIQUE INDEX IF NOT EXISTS ux_restrictions_in_force
    ON public.restrictions (user_id, channel_id, kind) WHERE lifted = FALSE;
