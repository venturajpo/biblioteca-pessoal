# Scripts de inicialização do banco

Os arquivos desta pasta são montados em `/docker-entrypoint-initdb.d` no contêiner
do MariaDB (ver `compose.yaml`) e rodam **uma única vez**: na primeira subida, quando
o volume de dados `dados-mariadb` ainda está vazio.

## Regras

- Ordem de execução: **alfabética**. Use prefixo numérico (`01_`, `02_`, `03_`).
- Extensões aceitas pelo MariaDB: `.sql`, `.sql.gz`, `.sql.xz`, `.sql.zst` e `.sh`.
- O banco definido em `MARIADB_DATABASE` já vem selecionado — não é preciso `USE`.
- Prefira comandos idempotentes (`CREATE TABLE IF NOT EXISTS`).

## Reexecutar do zero

Os scripts são ignorados enquanto o volume existir. Para recriar o banco:

```bash
docker compose down -v && docker compose up -d
```

> `-v` apaga o volume e, com ele, **todos os dados** do banco local.
