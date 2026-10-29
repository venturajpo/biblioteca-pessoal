using System;

namespace BibliotecaPessoal.BackEnd.Entity;

public class LoanHistory
{
    public int Id { get; set; }

    public required string BookIsbn { get; set; }

    public required string RA { get; set; }

    public DateOnly LoanDate { get; set; } 

    public DateOnly DueDate { get; set; }

    public Book? Book { get; set; }

    public DateOnly? ReturnDate { get; set; }
}