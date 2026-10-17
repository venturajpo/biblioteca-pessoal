using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BibliotecaPessoal.BackEnd.Catalog;

public class OpenLibraryCatalog(HttpClient http, ILogger<OpenLibraryCatalog> logger) : IBookCatalog
{
    public string Name => "Open Library";

    private const string CoversById = "https://covers.openlibrary.org/b/id/";

    private const string SearchFields =
        "key,title,author_name,cover_i,editions,editions.title,editions.isbn,editions.cover_i";

    public async Task<IReadOnlyList<CatalogBook>> SearchAsync(
        CatalogSearchField field,
        string terms,
        int limit,
        CancellationToken ct)
    {
        var parameter = field switch
        {
            CatalogSearchField.Isbn => "isbn",
            CatalogSearchField.Title => "title",
            CatalogSearchField.Author => "author",
            _ => throw new ArgumentOutOfRangeException(nameof(field)),
        };

        var result = await GetJsonAsync<SearchResult>(
            $"search.json?{parameter}={Uri.EscapeDataString(terms)}&lang=pt&limit={limit * 2}&fields={SearchFields}",
            ct);

        var found = (result?.Docs ?? [])
            .Select(d => (Work: d, Edition: d.Editions?.Docs?.FirstOrDefault()))
            .Select(x => (x.Work, x.Edition, Isbn: PickIsbn(x.Edition?.Isbn)))
            .Where(x => x.Isbn is not null)
            .DistinctBy(x => x.Isbn)
            .Take(limit)
            .ToList();

        return await Task.WhenAll(found.Select(async x => new CatalogBook(
            Isbn: x.Isbn!,
            Title: x.Edition?.Title ?? x.Work.Title ?? string.Empty,
            Author: JoinAuthors(x.Work.AuthorName),
            Synopsis: null,
            PageCount: null,
            Cover: await CoverDownload.TryGetAsync(
                http, CoverUrl(x.Edition?.CoverId ?? x.Work.CoverId, "M"), logger, ct))));
    }

    public async Task<CatalogBook?> FindByIsbnAsync(string isbn, CancellationToken ct)
    {

        var data = await GetJsonAsync<Dictionary<string, BooksData>>(
            $"api/books?bibkeys=ISBN:{isbn}&jscmd=data&format=json", ct);
        if (data is null || !data.TryGetValue($"ISBN:{isbn}", out var book))
            return null;

        var cover = await CoverDownload.TryGetAsync(
            http, book.Cover?.Large ?? book.Cover?.Medium, logger, ct);

        return new CatalogBook(
            Isbn: PickIsbn(book.Identifiers?.Isbn13) ?? PickIsbn(book.Identifiers?.Isbn10) ?? isbn,
            Title: book.Title ?? string.Empty,
            Author: JoinAuthors(book.Authors?.Select(a => a.Name)),
            Synopsis: await FindDescriptionAsync(isbn, ct),
            PageCount: book.NumberOfPages is int pages and > 0 and <= ushort.MaxValue ? (ushort)pages : null,
            Cover: cover);
    }

    private async Task<string?> FindDescriptionAsync(string isbn, CancellationToken ct)
    {
        try
        {
            var edition = await GetJsonAsync<EditionOrWork>($"isbn/{isbn}.json", ct);
            var description = TextOf(edition?.Description);
            if (description is not null || edition?.Works is not { Count: > 0 } works)
                return description;

            var work = await GetJsonAsync<EditionOrWork>($"{works[0].Key.TrimStart('/')}.json", ct);
            return TextOf(work?.Description);
        }
        catch (CatalogUnavailableException e)
        {
            logger.LogWarning(e, "Open Library: sem sinopse para {Isbn}", isbn);
            return null;
        }
    }

    private async Task<T?> GetJsonAsync<T>(string url, CancellationToken ct) where T : class
    {
        try
        {
            using var response = await http.GetAsync(url, ct);
            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>(ct);
        }
        catch (Exception e) when (
            e is HttpRequestException or JsonException
            || (e is TaskCanceledException && !ct.IsCancellationRequested))
        {
            throw new CatalogUnavailableException($"Open Library: {e.Message}", e);
        }
    }

    private static string? TextOf(JsonElement? description) => description switch
    {
        { ValueKind: JsonValueKind.String } text => text.GetString(),
        { ValueKind: JsonValueKind.Object } obj when obj.TryGetProperty("value", out var value) =>
            value.GetString(),
        _ => null,
    };

    private static string? CoverUrl(long? coverId, string size) =>
        coverId is > 0 ? $"{CoversById}{coverId}-{size}.jpg" : null;

    private static string? JoinAuthors(IEnumerable<string?>? authors)
    {
        var names = authors?.Where(a => !string.IsNullOrWhiteSpace(a)).ToList();
        return names is { Count: > 0 } ? string.Join(", ", names) : null;
    }

    private static string? PickIsbn(IEnumerable<string>? isbns) =>
        isbns?
            .Where(i => i.Length is 13 or 10)
            .OrderBy(i => i.Length == 13 ? 0 : 1)
            .FirstOrDefault();

    private sealed record SearchResult(
        [property: JsonPropertyName("docs")] List<SearchDoc>? Docs);

    private sealed record SearchDoc(
        [property: JsonPropertyName("title")] string? Title,
        [property: JsonPropertyName("author_name")] List<string>? AuthorName,
        [property: JsonPropertyName("cover_i")] long? CoverId,
        [property: JsonPropertyName("editions")] EditionList? Editions);

    private sealed record EditionList(
        [property: JsonPropertyName("docs")] List<SearchEdition>? Docs);

    private sealed record SearchEdition(
        [property: JsonPropertyName("title")] string? Title,
        [property: JsonPropertyName("isbn")] List<string>? Isbn,
        [property: JsonPropertyName("cover_i")] long? CoverId);

    private sealed record BooksData(
        [property: JsonPropertyName("title")] string? Title,
        [property: JsonPropertyName("authors")] List<Named>? Authors,
        [property: JsonPropertyName("number_of_pages")] int? NumberOfPages,
        [property: JsonPropertyName("cover")] CoverLinks? Cover,
        [property: JsonPropertyName("identifiers")] Identifiers? Identifiers);

    private sealed record Named(
        [property: JsonPropertyName("name")] string? Name);

    private sealed record CoverLinks(
        [property: JsonPropertyName("medium")] string? Medium,
        [property: JsonPropertyName("large")] string? Large);

    private sealed record Identifiers(
        [property: JsonPropertyName("isbn_10")] List<string>? Isbn10,
        [property: JsonPropertyName("isbn_13")] List<string>? Isbn13);

    private sealed record EditionOrWork(
        [property: JsonPropertyName("description")] JsonElement? Description,
        [property: JsonPropertyName("works")] List<KeyRef>? Works);

    private sealed record KeyRef(
        [property: JsonPropertyName("key")] string Key);
}
