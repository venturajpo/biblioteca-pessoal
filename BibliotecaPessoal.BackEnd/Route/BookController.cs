using BibliotecaPessoal.BackEnd.DTO;
using Microsoft.AspNetCore.Mvc;
using Google.Apis.Books.v1;

namespace BibliotecaPessoal.BackEnd.Route;

[ApiController]
[Route("[controller]")]
public class BookController(BooksService booksService) : Controller
{
    private readonly BooksService _booksService = booksService;

    [HttpGet]
    public async Task<IActionResult> GetBook(
        [FromQuery(Name = "page")] int page,
        [FromQuery(Name = "page_size")] int? pageSize = 20,
        [FromQuery(Name = "sort_by")] string? sortByStr = "title",
        [FromQuery(Name = "sort_direction")] string? sortDirStr = "asc",
        [FromQuery(Name = "filter_by")] string? filterByStr = "",
        [FromQuery(Name = "filter")] string? filter = ""
        )
    {
        // TODO: Retrieve books from database
        // TODO: Ignore Synopsis and Review
        
        // TODO: Construct response
        var list = new List<BookDto>();
        var response = new BookListResponse
        {
            Books = list,
            Count = list.Count,
            PagesCount = 0 // TODO: Contar o total de páginas
        };

        return Ok(response);
    }

    [HttpGet("{isbn}")]
    public IActionResult GetBook(string isbn)
    {
        // TODO: Retrieve book from database
        
        // TODO: Construct response
        var book = new BookDto();

        return Ok(book);
    }

    [HttpPost]
    public async Task<IActionResult> PostBook([FromBody] BookRegisterDto request)
    {
        var isbn = request.ISBN;
        
        if (string.IsNullOrWhiteSpace(isbn))
        {
            return BadRequest("ISBN cannot be empty.");
        }

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
            
            // TODO: Add book to database. 

            return Ok();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
            return Problem();
        }
    }

    [HttpPost("~/UpdateProgress/{isbn}")]
    public async Task<IActionResult> UpdateProgress(string isbn, [FromBody] BookUpdateProgressDto request)
    {
        var pagesRead = request.PagesRead;
        
        // TODO: Get book from DB

        // if (pagesRead is < 0 or > book.TotalPages)
        //     return Problem();
        
        // TODO: Update book.PagesRead;
        
        return Ok();
    }
    
    [HttpPost("~/UpdateRating/{isbn}")]
    public async Task<IActionResult> UpdateRating(string isbn, [FromBody] BookUpdateRatingDto request)
    {
        var rating = request.Rating;

        if (rating is < 0.0 or > 10.0)
            return Problem();
        
        // TODO: Update book.Rating;
        
        return Ok();
    }
    
    [HttpPost("~/UpdateReview/{isbn}")]
    public async Task<IActionResult> UpdateReview(string isbn, [FromBody] BookUpdateReviewDto request)
    {
        var review = request.Review;
        
        // TODO: Update book.Review;
        
        return Ok();
    }
}