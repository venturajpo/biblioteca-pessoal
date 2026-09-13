namespace BibliotecaPessoal.BackEnd.DTO;

using System.Text.Json.Serialization;

public class BookListResponse
{
    [JsonPropertyName("count")]
    public int Count { get; set; }
    
    [JsonPropertyName("page_count")]
    public int PagesCount { get; set; }
    
    [JsonPropertyName("books")]
    public List<BookDto> Books;
}

public class BookDto
{
    [JsonPropertyName("isbn")]
    string ISBN { get; set; }
    
    [JsonPropertyName("title")]
    string Title { get; set; }
    
    [JsonPropertyName("author")]
    string Author { get; set; }
    
    [JsonPropertyName("progress")]
    double Progress { get; set; }
    
    [JsonPropertyName("pages_read")]
    int PagesRead { get; set; }
    
    [JsonPropertyName("pages_total")]
    int PagesTotal { get; set; }
    
    [JsonPropertyName("rating")]
    double Rating { get; set; }
    
    [JsonPropertyName("review")]
    string Review { get; set; }
    
    [JsonPropertyName("synopsis")]
    string Synopsis { get; set; }
}

public class BookRegisterDto
{
    [JsonPropertyName("isbn")]
    public string ISBN { get; set; }
}

public class BookUpdateProgressDto
{
    [JsonPropertyName("pages_read")]
    public int PagesRead { get; set; }
}

public class BookUpdateRatingDto
{
    [JsonPropertyName("rating")]
    public double Rating { get; set; }
}

public class BookUpdateReviewDto
{
    [JsonPropertyName("review")]
    public string Review { get; set; }
}