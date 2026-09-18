-- Adiciona a coluna `autor`, que o init.sql original nao tinha mas que o BookDto
-- expoe como "author" e o POST ja recebe da Google Books.
--
-- Rode este arquivo UMA VEZ na tabela que ja existe. Em um banco criado do zero
-- pelo init.sql atualizado a coluna ja vem, e este script nao e necessario.

USE biblioteca_pessoal;

ALTER TABLE livro
    ADD COLUMN autor VARCHAR(512) NULL AFTER nome;
