-- Esquema de la base de datos de JobTracker (SQLite).
-- No hace falta ejecutarlo a mano: la aplicación lo ejecuta sola al iniciar
-- (ver DatabaseContext.Initialize). Se incluye como referencia y para inspeccionar
-- la base con herramientas como DB Browser for SQLite.

CREATE TABLE IF NOT EXISTS Applications (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    Company         TEXT NOT NULL,
    Position        TEXT NOT NULL,
    ApplicationDate TEXT NOT NULL,   -- formato yyyy-MM-dd
    Link            TEXT,
    Salary          REAL,
    Currency        TEXT NOT NULL DEFAULT 'COP',
    Status          TEXT NOT NULL,   -- Applied | InReview | Interview | TechnicalTest | Offer | Rejected
    Notes           TEXT
);
