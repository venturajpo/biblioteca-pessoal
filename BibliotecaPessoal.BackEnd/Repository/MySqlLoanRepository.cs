using BibliotecaPessoal.BackEnd.Entity;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaPessoal.BackEnd.Repository;

public class MySqlLoanRepository(ApplicationDbContext dbContext) : ILoanRepository
{
    private ApplicationDbContext _dbContext => dbContext;
    private DbSet<LoanHistory> _loans => dbContext.LoanHistories;

    public async Task<List<LoanHistory>> GetByIsbnAsync(string isbn)
    {
        return await _loans.AsNoTracking()
            .Where(l => l.BookIsbn == isbn)
            .OrderByDescending(l => l.LoanDate)
            .ThenByDescending(l => l.Id)
            .ToListAsync();
    }

    public async Task<LoanHistory?> GetOpenAsync(string isbn)
    {
        return await _loans.AsNoTracking()
            .FirstOrDefaultAsync(l => l.BookIsbn == isbn && l.ReturnDate == null);
    }

    public async Task<bool> InsertAsync(LoanHistory loan)
    {
        try
        {
            await _loans.AddAsync(loan);
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }

        return true;
    }

    public async Task<bool> SetReturnDateAsync(int id, DateOnly returnDate)
    {
        try
        {
            return await _loans.Where(l => l.Id == id && l.ReturnDate == null)
                .ExecuteUpdateAsync(setters => setters.SetProperty(l => l.ReturnDate, returnDate)) > 0;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }
    }
}
