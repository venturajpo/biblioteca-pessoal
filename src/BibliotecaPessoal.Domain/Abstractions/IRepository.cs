namespace BibliotecaPessoal.Domain.Abstractions;

/// <summary>
/// Contrato genérico de persistência de uma raiz de agregado. A interface vive no
/// domínio; a implementação vive na infraestrutura (inversão de dependência).
/// </summary>
/// <typeparam name="TAggregateRoot">Raiz do agregado manipulada pelo repositório.</typeparam>
public interface IRepository<TAggregateRoot>
    where TAggregateRoot : Entity, IAggregateRoot
{
    Task<TAggregateRoot?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AdicionarAsync(TAggregateRoot raiz, CancellationToken cancellationToken = default);

    void Remover(TAggregateRoot raiz);
}
