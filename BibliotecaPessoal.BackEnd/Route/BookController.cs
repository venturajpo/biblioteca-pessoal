using BibliotecaPessoal.BackEnd.Catalog;
using BibliotecaPessoal.BackEnd.DTO;
using BibliotecaPessoal.BackEnd.Entity;
using BibliotecaPessoal.BackEnd.Repository;
using Microsoft.AspNetCore.Mvc;

namespace BibliotecaPessoal.BackEnd.Route;

[ApiController]
[Route("[controller]")]
public class BookController(
    IBookCatalog catalog,
    IBookRepository bookRepository,
    ILogger<BookController> logger) : Controller
{
    private readonly IBookCatalog _catalog = catalog;
    private readonly IBookRepository _bookRepository = bookRepository;
    private readonly ILogger<BookController> _logger = logger;

    private const int SearchLimit = 10;

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
                    "registered_at" => query.OrderBy(b => b.RegisteredDate),
                    "pages_read" => query.OrderBy(b => b.PagesRead),
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
                    "registered_at" => query.OrderByDescending(b => b.RegisteredDate),
                    "pages_read" => query.OrderByDescending(b => b.PagesRead),
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

    [HttpGet("search")]
    public async Task<IActionResult> SearchBooks(
        [FromQuery(Name = "filter_by")] string? filterByStr = null,
        [FromQuery(Name = "filter")] string? filterStr = null,
        CancellationToken ct = default)
    {
        if (filterByStr is null || string.IsNullOrWhiteSpace(filterStr))
        {
            return BadRequest("Filter and Filter fields must be specified.");
        }

        CatalogSearchField? field = filterByStr switch
        {
            "isbn" => CatalogSearchField.Isbn,
            "title" => CatalogSearchField.Title,
            "author" => CatalogSearchField.Author,
            _ => null
        };

        if (field is null)
        {
            return BadRequest($"Filter field {filterByStr} is not supported.");
        }

        var terms = field == CatalogSearchField.Isbn ? NormalizeIsbn(filterStr.Trim()) : filterStr.Trim();

        try
        {
            var found = await _catalog.SearchAsync(field.Value, terms, SearchLimit, ct);

            return Ok(new BookSearchResponse
            {
                Books = [.. found.Select(b => new BookSearchResultDto
                {
                    Isbn = b.Isbn,
                    Title = b.Title,
                    Author = b.Author,
                    Image = ToBase64(b.Cover),
                })],
            });
        }
        catch (CatalogUnavailableException e)
        {
            _logger.LogError(e, "Nenhum catalogo respondeu a busca por {Field} {Terms}", field, terms);
            return CatalogsUnavailable(e);
        }
    }

    [HttpPost]
    public async Task<IActionResult> PostBook(
        [FromBody] BookRegisterDto request,
        CancellationToken ct = default)
    {
        var isbn = NormalizeIsbn(request.ISBN);

        if (await _bookRepository.GetByIsbnAsync(isbn) is not null)
        {
            return Conflict($"Book with ISBN {isbn} is already registered.");
        }

        CatalogBook? found;
        try
        {
            found = await _catalog.FindByIsbnAsync(isbn, ct);
        }
        catch (CatalogUnavailableException e)
        {
            _logger.LogError(e, "Nenhum catalogo respondeu para o ISBN {Isbn}", isbn);
            return CatalogsUnavailable(e);
        }

        if (found is null)
        {
            return UnprocessableEntity($"No book found with ISBN: {isbn}");
        }

        Book book = new Book
        {
            Isbn = found.Isbn,
            Title = found.Title,
            Author = found.Author ?? string.Empty,
            Synopsis = found.Synopsis ?? string.Empty,
            Cover = found.Cover,
            RegisteredDate = DateOnly.FromDateTime(DateTime.UtcNow),
            PagesTotal = found.PageCount,
            PagesRead = 0,
            Rating = null,
            Review = null,
        };

        return await _bookRepository.InsertAsync(book)
            ? Ok(ToDto(book, false))
            : Conflict($"Book with ISBN {book.Isbn} is already registered.");
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

    [HttpDelete("{isbn}")]
    public async Task<IActionResult> DeleteBook(string isbn)
    {
        return await _bookRepository.DeleteAsync(NormalizeIsbn(isbn))
            ? NoContent()
            : NotFound();
    }

    private static string NormalizeIsbn(string isbn) =>
        isbn.Replace("-", string.Empty).Replace(" ", string.Empty);

    private ObjectResult CatalogsUnavailable(CatalogUnavailableException e) => Problem(
        title: "Could not reach the book catalogs.",
        detail: e.InnerException is AggregateException all
            ? string.Join(" | ", all.InnerExceptions.Select(x => x.Message))
            : e.Message,
        statusCode: StatusCodes.Status502BadGateway);

    private static string? ToBase64(byte[]? cover) =>
        cover is { Length: > 0 } ? Convert.ToBase64String(cover) : null;
    private static BookDto ToDto(Book book, bool getFull) => new()
    {
        Isbn = book.Isbn,
        Title = book.Title,
        Author = book.Author,
        Image = ToBase64(book.Cover),
        Progress = book.Progress,
        PagesRead = book.PagesRead,
        PagesTotal = book.PagesTotal,
        Rating = book.Rating,
        Review = getFull ? book.Review : null,
        Synopsis = getFull ? book.Synopsis : null,
    };
}
