using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaPessoal.BackEnd;

using BibliotecaPessoal.BackEnd.Repository;
using Google.Apis.Books.v1;
using Google.Apis.Services;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

        // Add services to the container.
        builder.Services.AddAuthorization();

        builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        // Register Google Books API
        builder.Services.AddSingleton<BooksService>(new BooksService(new BaseClientService.Initializer()
        {
            ApiKey = builder.Configuration["GoogleBooks:ApiKey"],
            ApplicationName = "BibliotecaPessoal"
        }));

        // Scoped: uma instancia por requisicao. O repositorio hoje e sem estado, mas
        // Singleton passaria a compartilhar estado entre usuarios no dia em que houver
        // uma transacao aqui dentro.
        builder.Services.AddScoped<IBookRepository, MySqlBookRepository>();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}
