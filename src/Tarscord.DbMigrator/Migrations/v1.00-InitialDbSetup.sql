CREATE TABLE IF NOT EXISTS public.event_infos
(
    id                 SERIAL PRIMARY KEY,
    event_organizer    VARCHAR(200),
    event_organizer_id VARCHAR(2000),
    event_name         VARCHAR(200) NOT NULL,
    event_date         TIMESTAMP,
    event_description  VARCHAR(200),
    is_active          BOOLEAN,
    created            TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated            TIMESTAMP             DEFAULT NULL
);