using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MVSoftware.Flexibill.Domain.Common;
using MVSoftware.Flexibill.Domain.Users;

namespace MVSoftware.Flexibill.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.OrganizationId).IsRequired();
        builder.Property(u => u.DisplayName).IsRequired().HasMaxLength(200);

        builder.Property(u => u.Email)
            .HasConversion(v => v.Value, v => EmailAddress.Of(v))
            .HasColumnName("Email")
            .HasMaxLength(320)
            .IsRequired();

        // Roles en BranchIds zijn collecties van primitieven (geen entities) achter een
        // private backing field zonder public setter - opgeslagen als comma-separated kolom
        // i.p.v. een aparte join-tabel/JSON-kolom. Het converter-modeltype moet exact het
        // blootgestelde IReadOnlyCollection<T>-type zijn (niet List<T>), anders wijst EF Core
        // de conversie af ("Converter for model type 'List<T>' cannot be used for ... because
        // its type is 'IReadOnlyCollection<T>'") - EF Core schrijft de teruggegeven List<T> via
        // het backing field prima weg, ook al is het blootgestelde type de interface.
        var rolesConverter = new ValueConverter<IReadOnlyCollection<UserRole>, string>(
            roles => string.Join(',', roles),
            value => value.Length == 0
                ? new List<UserRole>()
                : value.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(Enum.Parse<UserRole>).ToList());
        var rolesComparer = new ValueComparer<IReadOnlyCollection<UserRole>>(
            (a, b) => (a ?? new List<UserRole>()).SequenceEqual(b ?? new List<UserRole>()),
            a => a.Aggregate(17, (hash, role) => HashCode.Combine(hash, role)),
            a => a.ToList());

        builder.Property(u => u.Roles)
            .HasConversion(rolesConverter, rolesComparer)
            .HasColumnName("Roles")
            .HasMaxLength(400);

        var branchIdsConverter = new ValueConverter<IReadOnlyCollection<Guid>, string>(
            branchIds => string.Join(',', branchIds),
            value => value.Length == 0 ? new List<Guid>() : value.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(Guid.Parse).ToList());
        var branchIdsComparer = new ValueComparer<IReadOnlyCollection<Guid>>(
            (a, b) => (a ?? new List<Guid>()).SequenceEqual(b ?? new List<Guid>()),
            a => a.Aggregate(17, (hash, id) => HashCode.Combine(hash, id)),
            a => a.ToList());

        builder.Property(u => u.BranchIds)
            .HasConversion(branchIdsConverter, branchIdsComparer)
            .HasColumnName("BranchIds")
            .HasMaxLength(2000);

        builder.HasIndex(u => new { u.OrganizationId, u.Email }).IsUnique();
    }
}
