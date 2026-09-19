CREATE DATABASE IF NOT EXISTS biblioteca_pessoal
    CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;

USE biblioteca_pessoal;

CREATE TABLE IF NOT EXISTS livro (
    isbn VARCHAR(13) NOT NULL PRIMARY KEY,
    nome VARCHAR(512) NOT NULL,
    autor VARCHAR(512) NULL,
    sinopse TEXT NULL,
    imagem VARCHAR(500) NULL,
    data_cadastro DATE NOT NULL,
    paginas_total SMALLINT UNSIGNED NULL,
    paginas_lidas SMALLINT UNSIGNED NOT NULL DEFAULT 0,
    nota DECIMAL(3,1) NULL CHECK (nota BETWEEN 0 AND 10),
    anotacao TEXT NULL,
    INDEX idx_livro_nome (nome)
);
