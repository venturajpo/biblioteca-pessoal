using System.Data.Common;
using BibliotecaPessoal.BackEnd.Entity;
using MySqlConnector;

namespace BibliotecaPessoal.BackEnd.Repository;

public class MySqlBookRepository : IBookRepository
{
    private const string AllColumns =
        "isbn, nome, autor, sinopse, imagem, data_cadastro, paginas_total, paginas_lidas, nota, anotacao";

    // A listagem omite sinopse e anotacao: sao colunas TEXT e nao servem para nada
    // numa tela de lista, mas vem multiplicadas pelo tamanho da pagina.
    private const string ListColumns =
        "isbn, nome, autor, imagem, data_cadastro, paginas_total, paginas_lidas, nota";

    // Nome de coluna nao pode ser parametro de SQL, entao o valor que chega da query
    // string e usado apenas como CHAVE destes dicionarios. O que vai para o SQL e o
    // valor mapeado aqui, nunca o texto do usuario.
    private static readonly Dictionary<string, string> SortableColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["title"] = "nome",
        ["isbn"] = "isbn",
        ["author"] = "autor",
        ["rating"] = "nota",
        ["pages_read"] = "paginas_lidas",
        ["pages_total"] = "paginas_total",
        ["registered_at"] = "data_cadastro"
    };

    private static readonly Dictionary<string, string> FilterableColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["title"] = "nome",
        ["author"] = "autor",
        ["isbn"] = "isbn"
    };

    private const int MaxPageSize = 100;

    private readonly string _connectionString;

    public MySqlBookRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "Connection string 'Default' nao configurada. Grave em user secrets: ConnectionStrings:Default");
    }

    public async Task<Book?> GetByIsbnAsync(string isbn, CancellationToken ct = default)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT {AllColumns} FROM livro WHERE isbn = @isbn";
        command.Parameters.AddWithValue("@isbn", isbn);

        await using var reader = await command.ExecuteReaderAsync(ct);

        return await reader.ReadAsync(ct) ? Map(reader, includeText: true) : null;
    }

    public async Task<(IReadOnlyList<Book> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? sortBy,
        bool ascending,
        string? filterBy,
        string? filter,
        CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        if (!SortableColumns.TryGetValue(sortBy ?? string.Empty, out var sortColumn))
            sortColumn = "nome";

        var direction = ascending ? "ASC" : "DESC";

        string? filterColumn = null;
        if (!string.IsNullOrWhiteSpace(filter))
        {
            var requested = string.IsNullOrWhiteSpace(filterBy) ? "title" : filterBy;
            if (FilterableColumns.TryGetValue(requested, out var mapped))
                filterColumn = mapped;
        }

        var where = filterColumn is null ? string.Empty : $" WHERE {filterColumn} LIKE @filter";
        var filterValue = $"%{filter}%";

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        var items = new List<Book>();

        await using (var command = connection.CreateCommand())
        {
            command.CommandText =
                $"SELECT {ListColumns} FROM livro{where} ORDER BY {sortColumn} {direction} LIMIT @limit OFFSET @offset";
            command.Parameters.AddWithValue("@limit", pageSize);
            command.Parameters.AddWithValue("@offset", (page - 1) * pageSize);
            if (filterColumn is not null)
                command.Parameters.AddWithValue("@filter", filterValue);

            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                items.Add(Map(reader, includeText: false));
        }

        await using var countCommand = connection.CreateCommand();
        countCommand.CommandText = $"SELECT COUNT(*) FROM livro{where}";
        if (filterColumn is not null)
            countCommand.Parameters.AddWithValue("@filter", filterValue);

        var total = Convert.ToInt32(await countCommand.ExecuteScalarAsync(ct));

        return (items, total);
    }

    public async Task<bool> InsertAsync(Book book, CancellationToken ct = default)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandText =
            $"""
             INSERT INTO livro ({AllColumns})
             VALUES (@isbn, @nome, @autor, @sinopse, @imagem, @dataCadastro,
                     @paginasTotal, @paginasLidas, @nota, @anotacao)
             """;

        command.Parameters.AddWithValue("@isbn", book.Isbn);
        command.Parameters.AddWithValue("@nome", book.Title);
        command.Parameters.AddWithValue("@autor", book.Author ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@sinopse", book.Synopsis ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@imagem", book.ImageUrl ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@dataCadastro", book.RegisteredAt);
        command.Parameters.AddWithValue("@paginasTotal", book.PagesTotal ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@paginasLidas", book.PagesRead);
        command.Parameters.AddWithValue("@nota", book.Rating ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@anotacao", book.Review ?? (object)DBNull.Value);

        try
        {
            return await command.ExecuteNonQueryAsync(ct) == 1;
        }
        catch (MySqlException e) when (e.Number == 1062)
        {
            // 1062 = duplicate entry. A chave primaria fez o trabalho dela; quem chamou
            // traduz isso em 409 Conflict.
            return false;
        }
    }

    public Task<bool> UpdatePagesReadAsync(string isbn, int pagesRead, CancellationToken ct = default) =>
        UpdateColumnAsync("paginas_lidas", pagesRead, isbn, ct);

    public Task<bool> UpdateRatingAsync(string isbn, decimal rating, CancellationToken ct = default) =>
        UpdateColumnAsync("nota", rating, isbn, ct);

    public Task<bool> UpdateReviewAsync(string isbn, string? review, CancellationToken ct = default) =>
        UpdateColumnAsync("anotacao", review, isbn, ct);

    // O nome da coluna vem sempre de um literal do proprio codigo (ver os tres metodos
    // acima), nunca da requisicao, por isso a interpolacao aqui e segura.
    private async Task<bool> UpdateColumnAsync(string column, object? value, string isbn, CancellationToken ct)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandText = $"UPDATE livro SET {column} = @value WHERE isbn = @isbn";
        command.Parameters.AddWithValue("@value", value ?? DBNull.Value);
        command.Parameters.AddWithValue("@isbn", isbn);

        if (await command.ExecuteNonQueryAsync(ct) > 0)
            return true;

        // Zero linhas afetadas tem duas causas: o ISBN nao existe, ou existe e o valor
        // enviado era igual ao que ja estava gravado. Sem distinguir as duas, gravar a
        // mesma nota duas vezes responderia 404 na segunda.
        return await ExistsAsync(connection, isbn, ct);
    }

    private static async Task<bool> ExistsAsync(MySqlConnection connection, string isbn, CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM livro WHERE isbn = @isbn";
        command.Parameters.AddWithValue("@isbn", isbn);

        return await command.ExecuteScalarAsync(ct) is not null;
    }

    private static Book Map(DbDataReader reader, bool includeText)
    {
        var book = new Book
        {
            Isbn = reader.GetString(reader.GetOrdinal("isbn")),
            Title = reader.GetString(reader.GetOrdinal("nome")),
            Author = GetNullableString(reader, "autor"),
            ImageUrl = GetNullableString(reader, "imagem"),
            RegisteredAt = DateOnly.FromDateTime(reader.GetDateTime(reader.GetOrdinal("data_cadastro"))),
            PagesTotal = GetNullableInt(reader, "paginas_total"),
            PagesRead = reader.GetInt32(reader.GetOrdinal("paginas_lidas")),
            Rating = GetNullableDecimal(reader, "nota")
        };

        if (includeText)
        {
            book.Synopsis = GetNullableString(reader, "sinopse");
            book.Review = GetNullableString(reader, "anotacao");
        }

        return book;
    }

    // NULL do banco chega como DBNull.Value, nao como null: chamar GetString ou
    // GetDecimal direto numa coluna nula lanca excecao.
    private static string? GetNullableString(DbDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static int? GetNullableInt(DbDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
    }

    private static decimal? GetNullableDecimal(DbDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetDecimal(ordinal);
    }
}
