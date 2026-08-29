namespace BibliotecaPessoal.Application.Abstractions;

/// <summary>
/// Confirma, em uma única transação, todas as alterações feitas nos agregados
/// durante o caso de uso. Implementado na camada de infraestrutura.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SalvarAlteracoesAsync(CancellationToken cancellationToken = default);
}
