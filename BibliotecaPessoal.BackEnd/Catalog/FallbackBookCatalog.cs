namespace BibliotecaPessoal.BackEnd.Catalog;

// Tenta os catalogos na ordem da lista. Passa para o proximo quando um esta
// indisponivel e tambem quando nao acha nada: um catalogo pode nao ter aquela edicao
public class FallbackBookCatalog(
    IReadOnlyList<IBookCatalog> catalogs,
    ILogger<FallbackBookCatalog> logger) : IBookCatalog
{
    public string Name => string.Join(" -> ", catalogs.Select(c => c.Name));

    public Task<IReadOnlyList<CatalogBook>> SearchAsync(
        CatalogSearchField field,
        string terms,
        int limit,
        CancellationToken ct) =>
        FirstFoundAsync(
            catalog => catalog.SearchAsync(field, terms, limit, ct),
            books => books.Count > 0,
            []);

    public Task<CatalogBook?> FindByIsbnAsync(string isbn, CancellationToken ct) =>
        FirstFoundAsync(
            catalog => catalog.FindByIsbnAsync(isbn, ct),
            book => book is not null,
            null);

    private async Task<T> FirstFoundAsync<T>(
        Func<IBookCatalog, Task<T>> query,
        Func<T, bool> found,
        T nothing)
    {
        var failures = new List<Exception>();

        foreach (var catalog in catalogs)
        {
            try
            {
                var result = await query(catalog);
                if (found(result))
                    return result;

                logger.LogInformation("{Catalog} nao encontrou nada", catalog.Name);
            }
            catch (CatalogUnavailableException e)
            {
                logger.LogWarning(e, "{Catalog} indisponivel", catalog.Name);
                failures.Add(e);
            }
        }

        // Se todos falharam, a consulta falhou; se algum respondeu vazio, o vazio vale.
        if (failures.Count == catalogs.Count)
            throw new CatalogUnavailableException(
                "Nenhum catalogo de livros respondeu.", new AggregateException(failures));

        return nothing;
    }
}
