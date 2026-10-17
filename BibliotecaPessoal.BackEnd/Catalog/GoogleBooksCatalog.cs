using Google;
using Google.Apis.Books.v1;
using Google.Apis.Books.v1.Data;

namespace BibliotecaPessoal.BackEnd.Catalog;

public class GoogleBooksCatalog(
    BooksService booksService,
    HttpClient http,
    ILogger<GoogleBooksCatalog> logger) : IBookCatalog
{
    public string Name => "Google Books";

    public async Task<IReadOnlyList<CatalogBook>> SearchAsync(
        CatalogSearchField field,
        string terms,
        int limit,
        CancellationToken ct)
    {
        var query = field switch
        {
            CatalogSearchField.Isbn => $"isbn:{terms}",
            CatalogSearchField.Title => ScopeTerms("intitle", terms),
            CatalogSearchField.Author => ScopeTerms("inauthor", terms),
            _ => throw new ArgumentOutOfRangeException(nameof(field)),
        };

        var volumes = await ListAsync(query, limit * 2, ct);

        var found = volumes
            .Where(v => v.VolumeInfo is not null)
            .Select(v => (Info: v.VolumeInfo, Isbn: PickIsbn(v.VolumeInfo.IndustryIdentifiers)))
            .Where(v => v.Isbn is not null)
            .DistinctBy(v => v.Isbn)
            .Take(limit)
            .ToList();

        return await Task.WhenAll(found.Select(async v =>
        {
            var links = v.Info.ImageLinks;
            var cover = await DownloadAsync(links?.Thumbnail ?? links?.SmallThumbnail, ct);
            return ToCatalogBook(v.Isbn!, v.Info, cover);
        }));
    }

    public async Task<CatalogBook?> FindByIsbnAsync(string isbn, CancellationToken ct)
    {
        var volume = (await ListAsync($"isbn:{isbn}", 1, ct)).FirstOrDefault();
        if (volume?.VolumeInfo is null)
            return null;

        var links = await GetImageLinksAsync(volume.Id, ct) ?? volume.VolumeInfo.ImageLinks;
        var cover = await DownloadAsync(
            links?.Small ?? links?.Thumbnail ?? links?.SmallThumbnail, ct);

        return ToCatalogBook(PickIsbn(volume.VolumeInfo.IndustryIdentifiers) ?? isbn, volume.VolumeInfo, cover);
    }

    private async Task<IList<Volume>> ListAsync(string query, int maxResults, CancellationToken ct)
    {
        try
        {
            var request = booksService.Volumes.List(query);
            request.MaxResults = maxResults;
            request.PrintType = VolumesResource.ListRequest.PrintTypeEnum.BOOKS;

            var response = await request.ExecuteAsync(ct);
            return response.Items ?? [];
        }
        catch (Exception e) when (IsUnavailable(e, ct))
        {
            throw new CatalogUnavailableException($"Google Books: {e.Message}", e);
        }
    }

    private async Task<Volume.VolumeInfoData.ImageLinksData?> GetImageLinksAsync(
        string volumeId,
        CancellationToken ct)
    {
        try
        {
            var volume = await booksService.Volumes.Get(volumeId).ExecuteAsync(ct);
            return volume.VolumeInfo?.ImageLinks;
        }
        catch (Exception e) when (IsUnavailable(e, ct))
        {
            logger.LogWarning(e, "Google Books: sem os links de capa do volume {VolumeId}", volumeId);
            return null;
        }
    }

    private Task<byte[]?> DownloadAsync(string? url, CancellationToken ct) =>
        CoverDownload.TryGetAsync(
            http,
            url?.Replace("http://", "https://").Replace("&edge=curl", string.Empty),
            logger,
            ct);

    private static bool IsUnavailable(Exception e, CancellationToken ct) =>
        e is GoogleApiException or HttpRequestException
        || (e is TaskCanceledException && !ct.IsCancellationRequested);

    private static CatalogBook ToCatalogBook(
        string isbn,
        Volume.VolumeInfoData info,
        byte[]? cover) => new(
        Isbn: isbn,
        Title: info.Title ?? string.Empty,
        Author: info.Authors is { Count: > 0 } authors ? string.Join(", ", authors) : null,
        Synopsis: info.Description,
        PageCount: info.PageCount is int pages and > 0 and <= ushort.MaxValue ? (ushort)pages : null,
        Cover: cover);

    private static string ScopeTerms(string field, string terms) =>
        string.Join(' ', terms
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => $"{field}:{t}"));

    private static string? PickIsbn(IList<Volume.VolumeInfoData.IndustryIdentifiersData>? identifiers) =>
        identifiers?
            .Where(i => i.Type is "ISBN_13" or "ISBN_10")
            .OrderBy(i => i.Type == "ISBN_13" ? 0 : 1)
            .Select(i => i.Identifier)
            .FirstOrDefault();
}
