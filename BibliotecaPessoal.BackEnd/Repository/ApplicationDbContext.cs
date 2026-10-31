using BibliotecaPessoal.BackEnd.Entity;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaPessoal.BackEnd.Repository;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Book> BookSet { get; set; }
    
    public DbSet<LoanHistory> LoanHistories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new BookConfiguration());
        modelBuilder.ApplyConfiguration(new LoanHistoryConfiguration());

        base.OnModelCreating(modelBuilder);
    }
}