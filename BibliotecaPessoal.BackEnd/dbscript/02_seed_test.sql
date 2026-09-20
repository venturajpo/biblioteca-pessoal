-- Dados de teste para a tabela `livro`.
-- Este arquivo NAO faz parte do script de inicializacao: o init.sql cria a
-- estrutura, este aqui so povoa o banco com linhas descartaveis para voce
-- testar a leitura antes de existir codigo C# que escreva no banco.

USE biblioteca_pessoal;

-- ---------------------------------------------------------------------------
-- Linha 1: completa. Todas as nove colunas preenchidas.
-- Serve para testar a leitura do caso "tudo presente".
-- ---------------------------------------------------------------------------
INSERT INTO book
    (isbn, title, author, synopsis, cover, register_date, pages_total, pages_read, rating, review)
VALUES
    ('9788535902778',
     'Dom Casmurro',
     '',
     'Bentinho relembra a juventude e o casamento com Capitu, remoendo a suspeita de uma traicao que nunca se confirma.',
     null,
     '2026-09-01',
     256,
     120,
     9.5,
     'Reler o capitulo dos olhos de ressaca.');

-- ---------------------------------------------------------------------------
-- Linha 2: sem nota e sem anotacao.
-- Repare que as duas colunas simplesmente nao aparecem na lista de colunas,
-- e por isso nao aparecem tambem na lista de VALUES. Como ambas aceitam NULL
-- e nao tem DEFAULT, o banco grava NULL nas duas.
-- Serve para testar o caminho "coluna nula" na leitura (IsDBNull).
-- ---------------------------------------------------------------------------
INSERT INTO book
(isbn, title, author, synopsis, cover, register_date, pages_total, pages_read)
VALUES
    ('9788533613379',
     'O Senhor dos Aneis: A Sociedade do Anel',
     '',
     'Frodo herda um anel magico e parte de sua terra natal para destrui-lo antes que caia nas maos do inimigo.',
     null,
     '2026-09-10',
     576,
     40);

-- ---------------------------------------------------------------------------
-- Linha 3: sem paginas_lidas.
-- Essa e a linha que testa o DEFAULT 0: a coluna foi omitida, entao o banco
-- deve preencher 0 sozinho. Tambem tem paginas_total NULL, simulando um livro
-- que a Google Books devolveu sem o campo pageCount.
-- Serve para testar o Progresso com divisao por zero e com nulo.
-- ---------------------------------------------------------------------------
INSERT INTO book
    (isbn, title, synopsis, cover, register_date, pages_total)
VALUES
    ('9788575225103',
     'Estruturas de Dados e Algoritmos',
     NULL,
     NULL,
     '2026-09-17',
     NULL);

-- ---------------------------------------------------------------------------
-- Conferencia
-- ---------------------------------------------------------------------------
SELECT isbn, title, pages_total, pages_read, rating FROM book;

-- O esperado:
--   linha 1 -> pages_read = 120, rating = 9.5
--   linha 2 -> rating e review em NULL
--   linha 3 -> pages_read = 0 (veio do DEFAULT), pages_total em NULL
