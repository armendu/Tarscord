CREATE TABLE IF NOT EXISTS public.EventInfos
(
    Id               SERIAL PRIMARY KEY,
    EventOrganizer   VARCHAR(200),
    EventOrganizerId INTEGER,
    EventName        VARCHAR(200) NOT NULL,
    EventDate        TIMESTAMP,
    EventDescription VARCHAR(200),
    IsActive         BOOLEAN,
    Created          TIMESTAMP    NOT NULL DEFAULT NOW(),
    Updated          TIMESTAMP             DEFAULT NULL
);