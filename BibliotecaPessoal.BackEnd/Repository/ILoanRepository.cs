using BibliotecaPessoal.BackEnd.Entity;

namespace BibliotecaPessoal.BackEnd.Repository;

public interface ILoanRepository
{
    /// <summary>Todos os emprestimos do livro, do mais recente para o mais antigo.</summary>
    Task<List<LoanHistory>> GetByIsbnAsync(string isbn);

    /// <summary>O emprestimo ainda sem devolucao (ReturnDate nulo), se houver.</summary>
    Task<LoanHistory?> GetOpenAsync(string isbn);

    Task<bool> InsertAsync(LoanHistory loan);

    Task<bool> SetReturnDateAsync(int id, DateOnly returnDate);
}
