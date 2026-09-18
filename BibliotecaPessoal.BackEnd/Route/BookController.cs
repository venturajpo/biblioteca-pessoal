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
    public async Task<IActionResult> GetBooks(
        [FromQuery(Name = "page")] int page = 1,
        [FromQuery(Name = "page_size")] int pageSize = 20,
        [FromQuery(Name = "sort_by")] string? sortBy = "title",
        [FromQuery(Name = "sort_direction")] string? sortDirection = "asc",
        [FromQuery(Name = "filter_by")] string? filterBy = null,
        [FromQuery(Name = "filter")] string? filter = null,
        CancellationToken ct = default)
    {
        var ascending = !string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        var (books, total) = await _bookRepository.GetPagedAsync(
            page, pageSize, sortBy, ascending, filterBy, filter, ct);

        var effectivePageSize = Math.Clamp(pageSize, 1, 100);

        return Ok(new BookListResponse
        {
            Books = books.Select(ToDto).ToList(),
            Count = total,
            PagesCount = (int)Math.Ceiling(total / (double)effectivePageSize)
        });
    }

    [HttpGet("{isbn}")]
    public async Task<IActionResult> GetBook(string isbn, CancellationToken ct = default)
    {
        var book = await _bookRepository.GetByIsbnAsync(NormalizeIsbn(isbn), ct);

        return book is null ? NotFound() : Ok(ToDto(book));
    }

    [HttpPost]
    public async Task<IActionResult> PostBook([FromBody] BookRegisterDto request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.ISBN))
            return BadRequest("ISBN cannot be empty.");

        var isbn = NormalizeIsbn(request.ISBN);

        var listRequest = _booksService.Volumes.List($"isbn:{isbn}");
        listRequest.MaxResults = 1;

        Volumes response;
        try
        {
            response = await listRequest.ExecuteAsync(ct);
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

        if (response.Items == null || response.Items.Count == 0)
            return UnprocessableEntity($"No book found with ISBN: {isbn}");

        var info = response.Items[0].VolumeInfo;

        var book = new Book
        {
            Isbn = isbn,
            Title = info.Title ?? isbn,
            Author = info.Authors is { Count: > 0 } ? string.Join(", ", info.Authors) : null,
            Synopsis = info.Description,
            ImageUrl = info.ImageLinks?.Thumbnail,
            RegisteredAt = DateOnly.FromDateTime(DateTime.Today),
            PagesTotal = info.PageCount,
            PagesRead = 0
        };

        return await _bookRepository.InsertAsync(book, ct)
            ? Ok(ToDto(book))
            : Conflict($"Book with ISBN {isbn} is already registered.");
    }

    [HttpPost("~/UpdateProgress/{isbn}")]
    public async Task<IActionResult> UpdateProgress(
        string isbn,
        [FromBody] BookUpdateProgressDto request,
        CancellationToken ct = default)
    {
        var normalized = NormalizeIsbn(isbn);

        var book = await _bookRepository.GetByIsbnAsync(normalized, ct);
        if (book is null)
            return NotFound();

        if (request.PagesRead < 0)
            return BadRequest("pages_read cannot be negative.");

        if (book.PagesTotal.HasValue && request.PagesRead > book.PagesTotal.Value)
            return BadRequest($"pages_read cannot exceed pages_total ({book.PagesTotal.Value}).");

        await _bookRepository.UpdatePagesReadAsync(normalized, request.PagesRead, ct);

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

        return await _bookRepository.UpdateRatingAsync(NormalizeIsbn(isbn), request.Rating, ct)
            ? Ok()
            : NotFound();
    }

    [HttpPost("~/UpdateReview/{isbn}")]
    public async Task<IActionResult> UpdateReview(
        string isbn,
        [FromBody] BookUpdateReviewDto request,
        CancellationToken ct = default)
    {
        return await _bookRepository.UpdateReviewAsync(NormalizeIsbn(isbn), request.Review, ct)
            ? Ok()
            : NotFound();
    }

    // O POST grava o ISBN normalizado, entao toda leitura precisa normalizar tambem,
    // senao "978-85-..." nunca casa com a chave primaria.
    private static string NormalizeIsbn(string isbn) =>
        isbn.Replace("-", string.Empty).Replace(" ", string.Empty);

    private static BookDto ToDto(Book book) => new()
    {
        Isbn = book.Isbn,
        Title = book.Title,
        Author = book.Author,
        Image = book.ImageUrl,
        Progress = book.Progress,
        PagesRead = book.PagesRead,
        PagesTotal = book.PagesTotal,
        Rating = book.Rating,
        Review = book.Review,
        Synopsis = book.Synopsis
    };
}
