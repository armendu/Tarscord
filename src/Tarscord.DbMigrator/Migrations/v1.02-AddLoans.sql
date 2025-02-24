CREATE TABLE IF NOT EXISTS public.loans
(
    id             SERIAL PRIMARY KEY NOT NULL,
    loaned_from_id BIGINT,
    loaned_from    VARCHAR(200),
    loaned_to_id   BIGINT,
    loaned_to      VARCHAR(200),
    description    VARCHAR(1000),
    amount_loaned  BIGINT             NOT NULL DEFAULT 0,
    amount_payed   BIGINT,
    confirmed      BOOLEAN            NOT NULL DEFAULT FALSE,
    created        TIMESTAMP          NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated        TIMESTAMP                   DEFAULT NULL
);