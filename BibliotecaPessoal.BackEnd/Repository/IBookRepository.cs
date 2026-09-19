using BibliotecaPessoal.BackEnd.Entity;

namespace BibliotecaPessoal.BackEnd.Repository;

public interface IBookRepository
{
    Task<Book?> GetByIsbnAsync(string isbn, CancellationToken ct = default);

    Task<(IReadOnlyList<Book> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? sortBy,
        bool ascending,
        string? filterBy,
        string? filter,
        CancellationToken ct = default);

    /// <summary>Retorna false quando o ISBN ja existe.</summary>
    Task<bool> InsertAsync(Book book, CancellationToken ct = default);

    /// <summary>Retorna false quando o ISBN nao existe.</summary>
    Task<bool> UpdatePagesReadAsync(string isbn, int pagesRead, CancellationToken ct = default);

    /// <summary>Retorna false quando o ISBN nao existe.</summary>
    Task<bool> UpdateRatingAsync(string isbn, decimal rating, CancellationToken ct = default);

    /// <summary>Retorna false quando o ISBN nao existe.</summary>
    Task<bool> UpdateReviewAsync(string isbn, string? review, CancellationToken ct = default);
}
