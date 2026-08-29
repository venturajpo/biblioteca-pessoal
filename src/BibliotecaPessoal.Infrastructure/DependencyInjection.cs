using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BibliotecaPessoal.Infrastructure;

/// <summary>
/// Registra as implementações concretas das portas declaradas na camada de
/// aplicação: banco de dados (MariaDB), repositórios e clientes de APIs externas.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Nome da cadeia de conexão esperada em <c>appsettings.json</c> ou na variável
    /// de ambiente <c>ConnectionStrings__BibliotecaPessoal</c>.</summary>
    public const string NomeDaConexao = "BibliotecaPessoal";

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var cadeiaDeConexao = configuration.GetConnectionString(NomeDaConexao)
            ?? throw new InvalidOperationException(
                $"A cadeia de conexão '{NomeDaConexao}' não foi configurada.");

        // Reservado para o DbContext (EF Core + Pomelo.EntityFrameworkCore.MySql),
        // os repositórios e o cliente HTTP do catálogo externo de livros.
        // A configuração efetiva depende da modelagem de dados, ainda pendente.
        _ = cadeiaDeConexao;

        return services;
    }
}
