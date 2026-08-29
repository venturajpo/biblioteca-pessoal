using Microsoft.AspNetCore.Mvc;

namespace BibliotecaPessoal.Api.Controllers;

/// <summary>
/// Endpoint mínimo de diagnóstico. Serve para o front-end confirmar, ainda no
/// esqueleto do projeto, que a comunicação Angular → API está funcionando.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class StatusController(IWebHostEnvironment ambiente) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<RespostaDeStatus>(StatusCodes.Status200OK)]
    public ActionResult<RespostaDeStatus> Obter() => Ok(new RespostaDeStatus(
        Aplicacao: "Biblioteca Pessoal API",
        Versao: typeof(StatusController).Assembly.GetName().Version?.ToString() ?? "0.0.0",
        Ambiente: ambiente.EnvironmentName,
        ConsultadoEm: DateTimeOffset.UtcNow));
}

/// <param name="Aplicacao">Nome da aplicação.</param>
/// <param name="Versao">Versão do assembly em execução.</param>
/// <param name="Ambiente">Ambiente de hospedagem (Development, Production...).</param>
/// <param name="ConsultadoEm">Instante da consulta, em UTC.</param>
public sealed record RespostaDeStatus(
    string Aplicacao,
    string Versao,
    string Ambiente,
    DateTimeOffset ConsultadoEm);
