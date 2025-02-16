CREATE TABLE IF NOT EXISTS public.event_attendees
(
    id            SERIAL PRIMARY KEY,
    attendee_id   INTEGER,
    event_info_id INTEGER,
    attendee_name VARCHAR(100),
    confirmed     BOOLEAN,
    created       TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated       TIMESTAMP          DEFAULT NULL,
    FOREIGN KEY (event_info_id) REFERENCES public.event_infos (id) ON DELETE CASCADE
);