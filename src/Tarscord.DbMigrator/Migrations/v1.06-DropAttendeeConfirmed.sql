-- Nothing ever set this to false: cancelling an attendance deletes the row, so the row existing was
-- already the confirmation. Three queries filtered on it and could never exclude anything.
ALTER TABLE public.event_attendees DROP COLUMN IF EXISTS confirmed;
