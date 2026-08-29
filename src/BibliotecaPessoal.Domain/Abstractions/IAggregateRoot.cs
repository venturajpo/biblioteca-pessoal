namespace BibliotecaPessoal.Domain.Abstractions;

/// <summary>
/// Marca a entidade que é a raiz de um agregado. Somente raízes de agregado
/// podem ser carregadas e persistidas diretamente por um repositório.
/// </summary>
public interface IAggregateRoot;
