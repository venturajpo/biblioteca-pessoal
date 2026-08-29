using Microsoft.Extensions.DependencyInjection;

namespace BibliotecaPessoal.Application;

/// <summary>
/// Ponto único de registro dos serviços da camada de aplicação (casos de uso,
/// validadores, mapeamentos). Mantém o <c>Program.cs</c> da API enxuto.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Os casos de uso serão registrados aqui conforme forem criados.
        return services;
    }
}
