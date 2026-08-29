# Biblioteca Pessoal

Sistema de biblioteca pessoal desenvolvido para o Projeto Integrador II da UNIVESP.

O usuário cadastra os livros que possui, está lendo ou já leu, com os dados
obtidos de um catálogo externo (Google Books / Open Library), registra avaliações
e progresso de leitura e acompanha estatísticas sobre seus hábitos.

> **Estado atual:** esqueleto inicial. A modelagem de dados ainda não foi definida —
> as camadas estão criadas e conectadas, mas ainda sem agregados nem casos de uso.

## Stack

| Camada          | Tecnologia                       |
| --------------- | -------------------------------- |
| Back-end        | C# / ASP.NET Core 10 (Web API)   |
| Front-end       | TypeScript / Angular 20          |
| Banco de dados  | MariaDB 11.4                     |
| Testes          | xUnit (back-end), Karma + Jasmine (front-end) |
| Contêineres     | Docker + Docker Compose          |

## Estrutura do repositório

```
biblioteca-pessoal/
├── BibliotecaPessoal.sln
├── compose.yaml                    # banco + api + front
├── Dockerfile                      # imagem da API
├── .env.example                    # modelo das variáveis do Compose
├── init/                           # scripts SQL da primeira execução
│   └── 01_estrutura_inicial.sql
├── src/
│   ├── BibliotecaPessoal.Domain/           # regras de negócio puras
│   ├── BibliotecaPessoal.Application/      # casos de uso e portas
│   ├── BibliotecaPessoal.Infrastructure/   # banco, repositórios, APIs externas
│   ├── BibliotecaPessoal.Api/              # controllers e composição
│   └── web/                                # front-end Angular
└── tests/
    └── BibliotecaPessoal.UnitTests/        # testes de unidade (xUnit)
```

## Arquitetura

Clean Architecture com os blocos táticos de DDD. A regra fundamental é a
**direção das dependências**, sempre de fora para dentro:

```
Api  ──►  Infrastructure  ──►  Application  ──►  Domain
 └────────────────────────────────┘
```

- **Domain** — entidades, objetos de valor e invariantes. Não referencia nenhum
  outro projeto nem pacote de infraestrutura. Contém as abstrações `Entity`,
  `ValueObject`, `IAggregateRoot`, `IRepository<T>` e `DomainException`.
- **Application** — orquestra os casos de uso e declara as *portas* de que precisa
  (`IUnitOfWork`, `ICatalogoExternoDeLivros`). Não sabe qual banco ou qual API
  externa serão usados.
- **Infrastructure** — implementa essas portas: acesso ao MariaDB, repositórios e
  o cliente HTTP do catálogo de livros.
- **Api** — expõe os endpoints HTTP e monta o contêiner de injeção de dependência
  (`AddApplication()` + `AddInfrastructure()`).

**Idioma do código:** termos técnicos em inglês (`Domain`, `Abstractions`,
`Repository`); a linguagem ubíqua do negócio em português (`Livros`, `Leituras`,
`ObterPorIdAsync`), como recomenda o DDD.

## Como executar

### Tudo em contêiner (recomendado para uma primeira execução)

```bash
cp .env.example .env
```

```bash
docker compose up --build
```

- Front-end: <http://localhost:4200>
- API: <http://localhost:8080/api/status>
- Sonda de saúde: <http://localhost:8080/health>

Na **primeira subida**, com o volume do banco ainda vazio, o MariaDB executa os
scripts de `init/` em ordem alfabética. Depois disso eles são ignorados. Para
recriar o banco do zero (apagando os dados):

```bash
docker compose down -v && docker compose up --build
```

### Desenvolvimento local

Somente o banco em contêiner, API e front rodando na máquina:

```bash
docker compose up -d db
```

```bash
dotnet run --project src/BibliotecaPessoal.Api
```

```bash
cd src/web && npm install && npm start
```

O `ng serve` usa `proxy.conf.json` para encaminhar `/api` até a API em
`http://localhost:5261`, evitando problemas de CORS no dia a dia.

## Testes

```bash
dotnet test
```

```bash
cd src/web && npm test
```

## Acessibilidade

O esqueleto já parte de uma base acessível, requisito do projeto:

- `lang="pt-BR"` no documento e marcos semânticos (`header`, `nav`, `main`, `footer`);
- link "Pular para o conteúdo principal" como primeiro elemento focável;
- `title` por rota, anunciado por leitores de tela a cada navegação;
- `aria-current="page"` no item de menu ativo e regiões `aria-live` para conteúdo dinâmico;
- foco visível (`:focus-visible`), suporte a tema claro/escuro e respeito a
  `prefers-reduced-motion`.

## Próximos passos

1. Modelar os agregados `Livro` e `Leitura` em `src/BibliotecaPessoal.Domain`.
2. Adicionar EF Core com o provedor `Pomelo.EntityFrameworkCore.MySql` em
   `Infrastructure/Persistence` e escrever `02_livros.sql` / `03_leituras.sql` em `init/`.
3. Implementar `ICatalogoExternoDeLivros` sobre a API do Google Books em
   `Infrastructure/ExternalApis`.
4. Construir as telas de estante, busca e estatísticas em `src/web/src/app/features`.
