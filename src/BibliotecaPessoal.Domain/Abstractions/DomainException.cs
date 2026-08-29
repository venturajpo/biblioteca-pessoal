namespace BibliotecaPessoal.Domain.Abstractions;

/// <summary>
/// Erro de regra de negócio. Sinaliza que uma invariante do domínio foi violada
/// e deve ser traduzida em uma resposta 4xx pela camada de apresentação.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string mensagem)
        : base(mensagem)
    {
    }

    public DomainException(string mensagem, Exception innerException)
        : base(mensagem, innerException)
    {
    }
}
