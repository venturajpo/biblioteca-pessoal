namespace BibliotecaPessoal.BackEnd.Catalog;

// Baixar a capa e o passo menos importante: se falhar, o livro so fica sem capa e a
// consulta continua valendo.
internal static class CoverDownload
{
    public static async Task<byte[]?> TryGetAsync(
        HttpClient http,
        string? url,
        ILogger logger,
        CancellationToken ct)
    {
        if (url is null)
            return null;

        try
        {
            using var response = await http.GetAsync(url, ct);
            var isImage = response.Content.Headers.ContentType?.MediaType?.StartsWith("image/") == true;
            if (!response.IsSuccessStatusCode || !isImage)
            {
                logger.LogWarning("Capa indisponivel em {Url}: HTTP {Status}", url, (int)response.StatusCode);
                return null;
            }

            return await response.Content.ReadAsByteArrayAsync(ct);
        }
        catch (Exception e) when (e is HttpRequestException || (e is TaskCanceledException && !ct.IsCancellationRequested))
        {
            logger.LogWarning(e, "Nao foi possivel baixar a capa {Url}", url);
            return null;
        }
    }
}
