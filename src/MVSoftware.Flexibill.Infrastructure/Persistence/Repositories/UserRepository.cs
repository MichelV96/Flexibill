using Microsoft.EntityFrameworkCore;
using MVSoftware.Flexibill.Application.Common.Interfaces;
using MVSoftware.Flexibill.Domain.Common;
using MVSoftware.Flexibill.Domain.Users;
using MVSoftware.Flexibill.Infrastructure.Persistence;

namespace MVSoftware.Flexibill.Infrastructure.Persistence.Repositories;

public sealed class UserRepository(FlexibillDbContext dbContext) : IUserRepository
{
    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        // RequestOtpCommand/ValidateOtpCommand roepen dit aan VOORDAT er een ingelogde gebruiker
        // (en dus een OrganizationId) bestaat - dat is precies het doel van deze lookup, de
        // gebruiker (en daarmee zijn tenant) vinden op basis van e-mailadres. De "Tenant"-
        // queryfilter (hoofdstuk 4.1) zou hier ICurrentUserContext.OrganizationId nodig hebben,
        // wat op dit punt nog niet bestaat - vandaar bewust IgnoreQueryFilters().
        var target = EmailAddress.Of(email);
        return dbContext.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == target, cancellationToken);
    }

    public Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken) =>
        dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

    public async Task<IReadOnlyList<User>> GetAllAsync(Guid organizationId, CancellationToken cancellationToken) =>
        await dbContext.Users.Where(u => u.OrganizationId == organizationId).ToListAsync(cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken)
    {
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken)
    {
        if (dbContext.Entry(user).State == EntityState.Detached)
        {
            dbContext.Users.Update(user);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
