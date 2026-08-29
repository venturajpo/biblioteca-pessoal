namespace BibliotecaPessoal.Application.Abstractions;

/// <summary>
/// Porta de saída para o catálogo externo de livros (Google Books / Open Library).
/// A camada de aplicação depende deste contrato, nunca do cliente HTTP concreto.
/// </summary>
public interface ICatalogoExternoDeLivros
{
    /// <summary>Busca livros no catálogo externo a partir de um termo livre (título, autor ou ISBN).</summary>
    Task<IReadOnlyList<LivroExterno>> BuscarAsync(string termo, CancellationToken cancellationToken = default);
}

/// <summary>Resultado bruto devolvido pelo catálogo externo, antes de virar um agregado do domínio.</summary>
/// <param name="Titulo">Título da obra.</param>
/// <param name="Autores">Autores creditados.</param>
/// <param name="Isbn">ISBN-13 quando disponível.</param>
/// <param name="UrlCapa">Endereço da imagem de capa quando disponível.</param>
public sealed record LivroExterno(
    string Titulo,
    IReadOnlyList<string> Autores,
    string? Isbn,
    string? UrlCapa);
