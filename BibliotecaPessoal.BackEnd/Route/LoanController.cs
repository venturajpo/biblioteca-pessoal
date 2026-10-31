using BibliotecaPessoal.BackEnd.DTO;
using BibliotecaPessoal.BackEnd.Entity;
using BibliotecaPessoal.BackEnd.Repository;
using Microsoft.AspNetCore.Mvc;

namespace BibliotecaPessoal.BackEnd.Route;

[ApiController]
[Route("[controller]")]
public class LoanController(
    IBookRepository bookRepository,
    ILoanRepository loanRepository) : Controller
{
    private readonly IBookRepository _bookRepository = bookRepository;
    private readonly ILoanRepository _loanRepository = loanRepository;

    // Mesmo limite da coluna RA.
    private const int MaxRaLength = 50;

    [HttpGet("{isbn}")]
    public async Task<IActionResult> GetLoans(string isbn)
    {
        var normalized = NormalizeIsbn(isbn);

        if (await _bookRepository.GetByIsbnAsync(normalized) is null)
            return NotFound();

        var loans = await _loanRepository.GetByIsbnAsync(normalized);

        return Ok(loans.Select(ToDto).ToList());
    }

    [HttpPost("{isbn}")]
    public async Task<IActionResult> PostLoan(string isbn, [FromBody] LoanRegisterDto request)
    {
        var normalized = NormalizeIsbn(isbn);
        var ra = request.RA.Trim();
        var today = DateOnly.FromDateTime(DateTime.Now);

        if (await _bookRepository.GetByIsbnAsync(normalized) is null)
            return NotFound();

        if (ra.Length == 0 || ra.Length > MaxRaLength)
            return BadRequest($"ra must have between 1 and {MaxRaLength} characters.");

        if (request.DueDate < today)
            return BadRequest("due_date cannot be before today.");

        // Um exemplar so pode estar com um aluno por vez.
        if (await _loanRepository.GetOpenAsync(normalized) is not null)
            return Conflict("Book is already on loan.");

        var loan = new LoanHistory
        {
            BookIsbn = normalized,
            RA = ra,
            LoanDate = today,
            DueDate = request.DueDate,
            ReturnDate = null,
        };

        return await _loanRepository.InsertAsync(loan)
            ? Ok(ToDto(loan))
            : Conflict("Could not register the loan.");
    }

    [HttpPost("{isbn}/return")]
    public async Task<IActionResult> ReturnBook(string isbn)
    {
        var open = await _loanRepository.GetOpenAsync(NormalizeIsbn(isbn));
        if (open is null)
            return NotFound();

        var today = DateOnly.FromDateTime(DateTime.Now);

        return await _loanRepository.SetReturnDateAsync(open.Id, today)
            ? Ok(ToDto(new LoanHistory
            {
                Id = open.Id,
                BookIsbn = open.BookIsbn,
                RA = open.RA,
                LoanDate = open.LoanDate,
                DueDate = open.DueDate,
                ReturnDate = today,
            }))
            : NotFound();
    }

    private static string NormalizeIsbn(string isbn) =>
        isbn.Replace("-", string.Empty).Replace(" ", string.Empty);

    private static LoanDto ToDto(LoanHistory loan) => new()
    {
        Id = loan.Id,
        RA = loan.RA,
        LoanDate = loan.LoanDate,
        DueDate = loan.DueDate,
        ReturnDate = loan.ReturnDate,
    };
}
