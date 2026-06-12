using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NeoBank.Domain.Entities;

namespace NeoBank.Infrastructure.Persistence.Configurations;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Balance)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(a => a.Currency)
            .HasConversion<string>()
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(a => a.AccountNumber)
            .HasMaxLength(29)
            .IsRequired();

        builder.HasIndex(a => a.AccountNumber)
            .IsUnique();

        // Enforce "one account per currency per user" at the database level.
        builder.HasIndex(a => new { a.UserId, a.Currency })
            .IsUnique();

        // Restrict deletes: a user with accounts cannot be removed.
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(a => a.RowVersion);
        builder.Ignore(a => a.DomainEvents);
    }
}