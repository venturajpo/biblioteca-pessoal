using BibliotecaPessoal.BackEnd.Entity;

namespace BibliotecaPessoal.BackEnd.Repository;

public interface IBookRepository
{
    Task<Book?> GetByIsbnAsync(string isbn);

    IQueryable<Book> QueryPaged(
        IQueryable<Book> query,
        int page,
        int pageSize);

    IQueryable<Book> QueryFilteredTitle(string title);
    IQueryable<Book> QueryFilteredAuthor(string author);
    IQueryable<Book> QueryFilteredRating(string ratingStr);
    IQueryable<Book> QueryUnFiltered();
    
    Task<bool> InsertAsync(Book book);

    Task<bool> UpdatePagesReadAsync(string isbn, ushort pagesRead);

    Task<bool> UpdateRatingAsync(string isbn, decimal rating);

    Task<bool> UpdateReviewAsync(string isbn, string? review);
}
