CREATE TABLE IF NOT EXISTS Loans
(
    Id                 SERIAL PRIMARY KEY NOT NULL,
    LoanedFrom         INTEGER,
    LoanedFromUsername VARCHAR(200),
    LoanedTo           INTEGER,
    LoanedToUsername   VARCHAR(200),
    Description        VARCHAR(1000),
    AmountLoaned       BIGINT             NOT NULL DEFAULT 0,
    AmountPayed        BIGINT,
    Confirmed          BOOLEAN            NOT NULL DEFAULT FALSE,
    Created            TIMESTAMP          NOT NULL DEFAULT NOW(),
    Updated            TIMESTAMP                   DEFAULT NULL
);