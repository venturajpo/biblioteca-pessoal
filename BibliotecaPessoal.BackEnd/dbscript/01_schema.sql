CREATE DATABASE IF NOT EXISTS biblioteca_pessoal;

USE biblioteca_pessoal;

CREATE TABLE IF NOT EXISTS book (
    isbn VARCHAR(13) NOT NULL PRIMARY KEY,
    title TEXT NOT NULL,
    author TEXT NOT NULL,
    synopsis TEXT NOT NULL,
    cover MEDIUMBLOB NULL,
    register_date DATE NOT NULL,
    pages_total SMALLINT UNSIGNED NULL,
    pages_read SMALLINT UNSIGNED NOT NULL DEFAULT 0,
    rating DECIMAL(3,1) NULL CHECK (rating BETWEEN 0 AND 10),
    review TEXT NULL,
    INDEX idx_book_title (title)
);
