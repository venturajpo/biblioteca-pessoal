namespace BibliotecaPessoal.BackEnd.DTO;

using System.Text.Json.Serialization;

public class LoanDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("ra")]
    public string RA { get; set; } = string.Empty;

    [JsonPropertyName("loan_date")]
    public DateOnly LoanDate { get; set; }

    [JsonPropertyName("due_date")]
    public DateOnly DueDate { get; set; }

    // Nulo = livro ainda emprestado. Sai como null no JSON (sem o WhenWritingNull)
    // para o front-end sempre receber o campo.
    [JsonPropertyName("return_date")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public DateOnly? ReturnDate { get; set; }
}

public class LoanRegisterDto
{
    [JsonPropertyName("ra")]
    public string RA { get; set; } = string.Empty;

    [JsonPropertyName("due_date")]
    public DateOnly DueDate { get; set; }
}
