namespace BibliotecaPessoal.BackEnd.Catalog;
public interface IBookCatalog
{
    string Name { get; }

    Task<IReadOnlyList<CatalogBook>> SearchAsync(
        CatalogSearchField field,
        string terms,
        int limit,
        CancellationToken ct);
    Task<CatalogBook?> FindByIsbnAsync(string isbn, CancellationToken ct);
}

public enum CatalogSearchField
{
    Isbn,
    Title,
    Author,
}

public record CatalogBook(
    string Isbn,
    string Title,
    string? Author,
    string? Synopsis,
    ushort? PageCount,
    byte[]? Cover);

public class CatalogUnavailableException(string message, Exception? inner = null)
    : Exception(message, inner);
