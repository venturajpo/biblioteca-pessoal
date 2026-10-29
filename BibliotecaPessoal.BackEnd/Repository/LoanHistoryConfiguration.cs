using BibliotecaPessoal.BackEnd.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BibliotecaPessoal.BackEnd.Repository;

public class LoanHistoryConfiguration : IEntityTypeConfiguration<LoanHistory>
{
    public void Configure(EntityTypeBuilder<LoanHistory> builder)
    {
        builder.ToTable("LoanHistory");

        builder.HasKey(lh => lh.Id);

        builder.Property(lh => lh.RA)
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(lh => lh.LoanDate)
               .IsRequired();

        builder.Property(lh => lh.DueDate)
               .IsRequired();
    }
}