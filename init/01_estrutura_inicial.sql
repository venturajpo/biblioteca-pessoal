-- ---------------------------------------------------------------------------
-- 01_estrutura_inicial.sql
--
-- Executado automaticamente pelo MariaDB na PRIMEIRA subida do contêiner
-- (a pasta init/ é montada em /docker-entrypoint-initdb.d).
-- O banco definido em MARIADB_DATABASE já vem selecionado, por isso não há
-- "USE" fixo aqui — o nome do banco continua configurável pelo .env.
--
-- Os arquivos rodam em ordem alfabética: use o prefixo numérico para versionar
-- (02_, 03_, ...). Para reexecutar do zero: docker compose down -v
-- ---------------------------------------------------------------------------

SET NAMES utf8mb4;

-- Registro das versões de esquema já aplicadas. Serve de ponto de controle
-- enquanto o projeto não adota uma ferramenta de migração (EF Core Migrations).
CREATE TABLE IF NOT EXISTS schema_version (
    id           INT UNSIGNED NOT NULL AUTO_INCREMENT,
    versao       VARCHAR(20)  NOT NULL,
    descricao    VARCHAR(255) NOT NULL,
    aplicado_em  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uk_schema_version_versao (versao)
) ENGINE = InnoDB
  DEFAULT CHARSET = utf8mb4
  COLLATE = utf8mb4_unicode_ci;

INSERT INTO schema_version (versao, descricao)
VALUES ('0.1.0', 'Estrutura inicial do projeto; modelagem de dominio ainda pendente')
ON DUPLICATE KEY UPDATE versao = versao;

-- ---------------------------------------------------------------------------
-- Próximos scripts (quando a modelagem estiver definida):
--   02_livros.sql       -> livro, autor, livro_autor
--   03_leituras.sql     -> leitura, progresso, avaliacao
-- ---------------------------------------------------------------------------
