namespace BibliotecaPessoal.BackEnd.DTO;

using System.Text.Json.Serialization;

public class BookListResponse
{
    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("page_count")]
    public int PagesCount { get; set; }

    // Tem de ser propriedade, nao campo: o System.Text.Json ignora campos por padrao.
    [JsonPropertyName("books")]
    public List<BookDto> Books { get; set; } = [];
}

public class BookDto
{
    [JsonPropertyName("isbn")]
    public string Isbn { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("author")]
    public string? Author { get; set; }

    [JsonPropertyName("image")]
    public string? Image { get; set; }

    [JsonPropertyName("progress")]
    public double Progress { get; set; }

    [JsonPropertyName("pages_read")]
    public int PagesRead { get; set; }

    [JsonPropertyName("pages_total")]
    public int? PagesTotal { get; set; }

    [JsonPropertyName("rating")]
    public decimal? Rating { get; set; }

    [JsonPropertyName("review")]
    public string? Review { get; set; }

    [JsonPropertyName("synopsis")]
    public string? Synopsis { get; set; }
}

public class BookRegisterDto
{
    [JsonPropertyName("isbn")]
    public string ISBN { get; set; } = string.Empty;
}

public class BookUpdateProgressDto
{
    [JsonPropertyName("pages_read")]
    public int PagesRead { get; set; }
}

public class BookUpdateRatingDto
{
    [JsonPropertyName("rating")]
    public decimal Rating { get; set; }
}

public class BookUpdateReviewDto
{
    [JsonPropertyName("review")]
    public string? Review { get; set; }
}
