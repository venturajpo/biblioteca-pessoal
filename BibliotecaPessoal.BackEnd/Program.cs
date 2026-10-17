using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaPessoal.BackEnd;

using BibliotecaPessoal.BackEnd.Catalog;
using BibliotecaPessoal.BackEnd.Repository;
using Google.Apis.Books.v1;
using Google.Apis.Http;
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
        var booksService = new BooksService(new BaseClientService.Initializer()
        {
            ApiKey = builder.Configuration["GoogleBooks:ApiKey"],
            ApplicationName = "BibliotecaPessoal",
            DefaultExponentialBackOffPolicy = ExponentialBackOffPolicy.None,
        });
        booksService.HttpClient.Timeout = TimeSpan.FromSeconds(10);
        builder.Services.AddSingleton(booksService);

        var contact = builder.Configuration["OpenLibrary:Contact"];
        var userAgent = string.IsNullOrWhiteSpace(contact)
            ? "BibliotecaPessoal/1.0"
            : $"BibliotecaPessoal/1.0 ({contact})";

        builder.Services.AddHttpClient<GoogleBooksCatalog>(client =>
            client.Timeout = TimeSpan.FromSeconds(10));
        builder.Services.AddHttpClient<OpenLibraryCatalog>(client =>
        {
            client.BaseAddress = new Uri("https://openlibrary.org/");
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
        });


        builder.Services.AddScoped<IBookCatalog>(services => new FallbackBookCatalog(
            [services.GetRequiredService<OpenLibraryCatalog>(), services.GetRequiredService<GoogleBooksCatalog>()],
            services.GetRequiredService<ILogger<FallbackBookCatalog>>()));

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
