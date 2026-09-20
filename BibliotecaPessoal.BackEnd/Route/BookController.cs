using BibliotecaPessoal.BackEnd.DTO;
using BibliotecaPessoal.BackEnd.Entity;
using BibliotecaPessoal.BackEnd.Repository;
using Microsoft.AspNetCore.Mvc;
using Google;
using Google.Apis.Books.v1;
using Google.Apis.Books.v1.Data;

namespace BibliotecaPessoal.BackEnd.Route;

[ApiController]
[Route("[controller]")]
public class BookController(
    BooksService booksService,
    IBookRepository bookRepository,
    ILogger<BookController> logger) : Controller
{
    private readonly BooksService _booksService = booksService;
    private readonly IBookRepository _bookRepository = bookRepository;
    private readonly ILogger<BookController> _logger = logger;

    [HttpGet]
    public async Task<IActionResult> GetBook(
        [FromQuery(Name = "page")] int page = 1,
        [FromQuery(Name = "page_size")] int pageSize = 20,
        [FromQuery(Name = "sort_by")] string? sortByStr = "title",
        [FromQuery(Name = "sort_direction")] string? sortDirStr = "asc",
        [FromQuery(Name = "filter_by")] string? filterByStr = null,
        [FromQuery(Name = "filter")] string? filterStr = null)
    {
        var ascending = !string.Equals(sortDirStr, "desc", StringComparison.OrdinalIgnoreCase);

        if (filterStr == null ^ filterByStr == null)
        {
            return BadRequest("Filter and Filter fields must be specified or leave both blank.");
        }

        if (sortByStr == null ^ sortDirStr == null)
        {
            return BadRequest("Sort and Sort fields must be specified or leave both blank.");
        }


        IQueryable<Book> query = filterByStr switch
        {
            "title" => _bookRepository.QueryFilteredTitle(filterStr),
            "author" => _bookRepository.QueryFilteredAuthor(filterStr),
            "rating" => _bookRepository.QueryFilteredRating(filterStr),
            null => _bookRepository.QueryUnFiltered(),
            _ => throw new ArgumentException($"Filter field {filterStr} is not supported.")
        };
        
        int count = query.Count();

        if (sortByStr is not null)
        {

            if (ascending)
            {
                query = sortByStr switch
                {
                    "title" => query.OrderBy(b => b.Title),
                    "author" => query.OrderBy(b => b.Author),
                    "progress" => query.OrderBy(b =>
                        (b.PagesTotal == 0 || b.PagesTotal == null) ? 0 : (double)b.PagesRead / b.PagesTotal),
                    "rating" => query.OrderBy(b => b.Rating ?? 0),
                    _ => throw new ArgumentException($"Sort field {sortByStr} is not supported.")
                };
            }
            else
            {
                query = sortByStr switch
                {
                    "title" => query.OrderByDescending(b => b.Title),
                    "author" => query.OrderByDescending(b => b.Author),
                    "progress" => query.OrderByDescending(b =>
                        (b.PagesTotal == 0 || b.PagesTotal == null) ? 100.0 : (double)b.PagesRead / b.PagesTotal),
                    "rating" => query.OrderByDescending(b => b.Rating ?? 10.0m),
                    _ => throw new ArgumentException($"Sort field {sortByStr} is not supported.")
                };
            }  
        }

        query = _bookRepository.QueryPaged(query, page, pageSize);


        try
        {
            List<BookDto> books = query.Select(b => ToDto(b, false)).ToList();

            return Ok(new BookListResponse
            {
                Books = books,
                Count = count,
                PagesCount = (int)Math.Ceiling(count / (double)pageSize)
            });
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpGet("{isbn}")]
    public async Task<IActionResult> GetBook(string isbn)
    {
        var book = await _bookRepository.GetByIsbnAsync(NormalizeIsbn(isbn));

        return book is null ? NotFound() : Ok(ToDto(book, true));
    }

    [HttpPost]
    public async Task<IActionResult> PostBook([FromBody] BookRegisterDto request)
    {
        var isbn = NormalizeIsbn(request.ISBN);

        // Clean up input (remove dashes or spaces often found in ISBNs)
        isbn = isbn.Replace("-", "").Replace(" ", "");

        try
        {
            string query = $"isbn:{isbn}";

            var listRequest = _booksService.Volumes.List(query);
            listRequest.MaxResults = 1;

            var response = await listRequest.ExecuteAsync();

            if (response.Items == null || response.Items.Count == 0)
            {
                return UnprocessableEntity($"No book found with ISBN: {isbn}");
            }

            var volume = response.Items[0];
            var info = volume.VolumeInfo;

            byte[] coverArt = [];
            
            using (HttpClient c = new HttpClient())
            {
                string? url = SortImageLinks(info.ImageLinks);
                if (url is not null)
                    using (var r = c.GetByteArrayAsync(url))
                    {
                        coverArt = r.Result;
                    }
            }

            Book book = new Book
            {
                Isbn = info.IndustryIdentifiers.OrderBy(i => SortIdentifier(i.Type)).First().Identifier,
                Title = info.Title,
                Author = string.Join(", ", info.Authors),
                Synopsis = info.Description,
                Cover = coverArt,
                RegisteredDate = DateOnly.FromDateTime(DateTime.UtcNow),
                PagesTotal = (ushort)info.PageCount,
                PagesRead = 0,
                Rating = null,
                Review = null,
            };


            return await _bookRepository.InsertAsync(book)
                ? Ok(ToDto(book, false))
                : Conflict($"Book with ISBN {isbn} is already registered.");
        }
        catch (GoogleApiException e)
        {
            // Falha da API externa nao deve derrubar a requisicao: 502 comunica que o
            // problema esta a jusante, e o detail carrega a mensagem da Google (chave
            // ausente, chave invalida, API nao habilitada no projeto, cota estourada).
            _logger.LogError(e, "Falha ao consultar a Google Books API para o ISBN {Isbn}", isbn);

            return Problem(
                title: "Could not reach the Google Books API.",
                detail: e.Message,
                statusCode: StatusCodes.Status502BadGateway);
        }
    }

    [HttpPost("~/UpdateProgress/{isbn}")]
    public async Task<IActionResult> UpdateProgress(
        string isbn,
        [FromBody] BookUpdateProgressDto request,
        CancellationToken ct = default)
    {
        var normalized = NormalizeIsbn(isbn);

        var book = await _bookRepository.GetByIsbnAsync(normalized);
        if (book is null)
            return NotFound();

        if (request.PagesRead < 0)
            return BadRequest("pages_read cannot be negative.");

        if (book.PagesTotal.HasValue && request.PagesRead > book.PagesTotal.Value)
            return BadRequest($"pages_read cannot exceed pages_total ({book.PagesTotal.Value}).");

        await _bookRepository.UpdatePagesReadAsync(normalized, (ushort)request.PagesRead);

        return Ok();
    }

    [HttpPost("~/UpdateRating/{isbn}")]
    public async Task<IActionResult> UpdateRating(
        string isbn,
        [FromBody] BookUpdateRatingDto request,
        CancellationToken ct = default)
    {
        if (request.Rating is < 0m or > 10m)
            return BadRequest("rating must be between 0 and 10.");

        return await _bookRepository.UpdateRatingAsync(NormalizeIsbn(isbn), request.Rating)
            ? Ok()
            : NotFound();
    }

    [HttpPost("~/UpdateReview/{isbn}")]
    public async Task<IActionResult> UpdateReview(
        string isbn,
        [FromBody] BookUpdateReviewDto request,
        CancellationToken ct = default)
    {
        return await _bookRepository.UpdateReviewAsync(NormalizeIsbn(isbn), request.Review)
            ? Ok()
            : NotFound();
    }

    // O POST grava o ISBN normalizado, entao toda leitura precisa normalizar tambem,
    // senao "978-85-..." nunca casa com a chave primaria.
    private static string NormalizeIsbn(string isbn) =>
        isbn.Replace("-", string.Empty).Replace(" ", string.Empty);

    private static int SortIdentifier(string identifier)
    {
        return identifier switch
        {
            "ISBN_13" => 0,
            "ISBN_10" => 2,
            "ISSN" => 4,
            _ => int.MaxValue
        };
    }

    private static string? SortImageLinks(Volume.VolumeInfoData.ImageLinksData images)
    {
        return images.ExtraLarge
                          ?? images?.Large
                          ?? images?.Medium
                          ?? images?.Thumbnail
                          ?? images?.SmallThumbnail
                          ?? null;
    }
    
    
    private static BookDto ToDto(Book book, bool getFull) => new()
    {
        Isbn = book.Isbn,
        Title = book.Title,
        Author = book.Author,
        Image = (getFull && book.Cover != null) ? Convert.ToBase64String(book.Cover) : null,
        Progress = book.Progress,
        PagesRead = book.PagesRead,
        PagesTotal = book.PagesTotal,
        Rating = book.Rating,
        Review = getFull ? book.Review : null,
        Synopsis = getFull ? book.Synopsis : null,
    };
}
