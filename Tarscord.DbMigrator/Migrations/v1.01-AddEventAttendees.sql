CREATE TABLE IF NOT EXISTS public.EventAttendees
(
    Id           SERIAL PRIMARY KEY,
    AttendeeId   INTEGER,
    EventInfoId  INTEGER,
    AttendeeName VARCHAR(100),
    Confirmed    BOOLEAN,
    Created      TIMESTAMP NOT NULL DEFAULT NOW(),
    Updated      TIMESTAMP          DEFAULT NULL,
    FOREIGN KEY (EventInfoId) REFERENCES public.EventInfos (Id) ON DELETE CASCADE
);