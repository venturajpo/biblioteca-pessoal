using BibliotecaPessoal.Application;
using BibliotecaPessoal.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

const string PoliticaCorsDoFrontEnd = "front-end-angular";

var origensPermitidas = builder.Configuration
    .GetSection("Cors:OrigensPermitidas")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
    options.AddPolicy(PoliticaCorsDoFrontEnd, policy => policy
        .WithOrigins(origensPermitidas)
        .AllowAnyHeader()
        .AllowAnyMethod()));

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

// Composição das camadas internas (Clean Architecture):
// a API conhece Application e Infrastructure; Domain não conhece ninguém.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Documento OpenAPI em /openapi/v1.json — consumido pelo Scalar/Swagger UI.
    app.MapOpenApi();
}

app.UseCors(PoliticaCorsDoFrontEnd);

// Sonda usada pelo Docker Compose para saber quando a API está pronta.
app.MapHealthChecks("/health");

app.MapControllers();

app.Run();
