namespace BibliotecaPessoal.BackEnd;
using Google.Apis.Books.v1;
using Google.Apis.Services;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddAuthorization();

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();
        
        // Register Google Books API
        builder.Services.AddSingleton<BooksService>(new BooksService(new BaseClientService.Initializer()
        {
            ApiKey = "AIzaSyBlRBZcH2TdfCB_p_6-hAXVkwSM3kcJ8bM",
            ApplicationName = "BibliotecaPessoal"
        }));

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.Run();
    }
}