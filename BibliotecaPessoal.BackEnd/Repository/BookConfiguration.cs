using BibliotecaPessoal.BackEnd.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BibliotecaPessoal.BackEnd.Repository;

public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.ToTable("book");

        builder.HasKey(b => b.Isbn);
        builder.Property(b => b.Isbn)
            .HasColumnName("isbn")
            .HasColumnType("varchar")
            .HasMaxLength(13);
        
        builder.Property(b => b.Title)
            .HasColumnName("title")
            .HasColumnType("TEXT")
            .IsRequired();
        
        builder.Property(b => b.Author)
            .HasColumnName("author")
            .HasColumnType("TEXT")
            .IsRequired();
        
        builder.Property(b => b.Synopsis)
            .HasColumnName("synopsis")
            .HasColumnType("TEXT")
            .IsRequired();
        
        builder.Property(b => b.Cover)
            .HasColumnName("cover")
            .HasColumnType("MEDIUMBLOB")
            .IsRequired(false);
        
        builder.Property(b => b.RegisteredDate)
            .HasColumnName("register_date")
            .HasColumnType("DATE")
            .IsRequired();
        
        builder.Property(b => b.PagesTotal)
            .HasColumnName("pages_total")
            .HasColumnType("SMALLINT UNSIGNED")
            .IsRequired(false);
        
        builder.Property(b => b.PagesRead)
            .HasColumnName("pages_read")
            .HasColumnType("SMALLINT UNSIGNED")
            .HasDefaultValue(0)
            .IsRequired();
        
        builder.Property(b => b.Rating)
            .HasColumnName("rating")
            .HasColumnType("DECIMAL")
            .HasPrecision(3, 1)
            .IsRequired(false);
        
        builder.Property(b => b.Review)
            .HasColumnName("review")
            .HasColumnType("TEXT")
            .IsRequired(false);

        builder.HasIndex(b => b.Title, "idx_book_title");

        builder.ToTable("book");
    }
}