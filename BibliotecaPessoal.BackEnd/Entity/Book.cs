namespace BibliotecaPessoal.BackEnd.Entity;

public class Book
{
    public required string Isbn { get; set; }

    public required string Title { get; set; }

    public string Author { get; set; }

    public string Synopsis { get; set; }

    public byte[]? Cover { get; set; }

    public DateOnly RegisteredDate { get; set; }

    public ushort? PagesTotal { get; set; }

    public ushort PagesRead { get; set; }

    public decimal? Rating { get; set; }

    public string? Review { get; set; }

    // Derivado, nunca persistido: guardar o progresso criaria uma segunda fonte
    // de verdade que pode discordar de PagesRead/PagesTotal.
    // O padrao "is > 0" cobre null e zero de uma vez, e o cast evita divisao inteira.
    public double Progress => PagesTotal is > 0
        ? Math.Round((double)PagesRead / PagesTotal.Value * 100, 2)
        : 0;
}