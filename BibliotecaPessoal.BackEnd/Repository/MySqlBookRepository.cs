using BibliotecaPessoal.BackEnd.Entity;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaPessoal.BackEnd.Repository;

public class MySqlBookRepository(ApplicationDbContext dbContext) : IBookRepository
{
    private ApplicationDbContext _dbContext => dbContext;
    private DbSet<Book> _books => dbContext.BookSet;

    public async Task<Book?> GetByIsbnAsync(string isbn)
    {
        return await _books.FirstOrDefaultAsync(b => b.Isbn == isbn);
    }

    public IQueryable<Book> QueryPaged(
        IQueryable<Book> query,
        int page,
        int pageSize
        )
    {
        return query.Skip(pageSize * (page - 1)).Take(pageSize);
    }

    public IQueryable<Book> QueryFilteredTitle(string title)
    {
        return _books.Where(b => b.Title == title);
    }

    public IQueryable<Book> QueryFilteredAuthor(string author)
    {
        return _books.Where(b => b.Author == author);
    }
    
    public IQueryable<Book> QueryFilteredRating(string ratingStr)
    {
        bool parsed = Decimal.TryParse(ratingStr, out decimal rating);
        if (!parsed)
            throw new ArgumentException("Invalid rating format");
        return _books.Where(b => b.Rating == rating);
    }

    public IQueryable<Book> QueryUnFiltered()
    {
        return _books.AsQueryable();
    }

    public async Task<bool> InsertAsync (Book book)
    {
        try
        {
            await _books.AddAsync(book);
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }

        return true;
    }

    public async Task<bool> UpdatePagesReadAsync(string isbn, ushort pagesRead)
    {
        try
        {
            return await _books.Where(b => b.Isbn == isbn)
                .ExecuteUpdateAsync(setters => setters.SetProperty(b => b.PagesRead, pagesRead)) > 0;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }
    }

    public async Task<bool> UpdateRatingAsync(string isbn, decimal rating)
    {        
        try
        {
            return await _books.Where(b => b.Isbn == isbn)
                .ExecuteUpdateAsync(setters => setters.SetProperty(b => b.Rating, rating)) > 0;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }
    }

    public async Task<bool> UpdateReviewAsync(string isbn, string? review)
    {        
        try
        {
            return await _books.Where(b => b.Isbn == isbn)
                .ExecuteUpdateAsync(setters => setters.SetProperty(b => b.Review, review)) > 0;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }
    }
}
