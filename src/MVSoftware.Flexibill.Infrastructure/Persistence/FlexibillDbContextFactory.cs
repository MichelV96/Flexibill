using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MVSoftware.Flexibill.Infrastructure.Persistence;

/// <summary>
/// Nodig voor `dotnet ef migrations add`/`dotnet ef database update`: op dat moment bestaat
/// er geen DI-container/HttpContext om <see cref="MVSoftware.Flexibill.Application.Common.Interfaces.ICurrentUserContext"/>
/// uit op te lossen. De waarden van de stub doen er niet toe - alleen de query-filter-
/// <em>expressie</em> wordt in het model vastgelegd, niet de waarde (zie
/// FlexibillDbContext.ApplyMultiTenancyQueryFilters).
/// </summary>
public sealed class FlexibillDbContextFactory : IDesignTimeDbContextFactory<FlexibillDbContext>
{
    public FlexibillDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<FlexibillDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=Flexibill;Trusted_Connection=True;TrustServerCertificate=True");

        return new FlexibillDbContext(optionsBuilder.Options, new SystemCurrentUserContext());
    }
}
