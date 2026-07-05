using Microsoft.EntityFrameworkCore.Storage;
using MVSoftware.Flexibill.Application.Common.Interfaces;

namespace MVSoftware.Flexibill.Infrastructure.Persistence;

public sealed class EfUnitOfWork(FlexibillDbContext dbContext) : IUnitOfWork
{
    public async Task<IDbContextTransactionHandle> BeginTransactionAsync(CancellationToken cancellationToken)
    {
        var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        return new EfDbContextTransactionHandle(transaction);
    }

    private sealed class EfDbContextTransactionHandle(IDbContextTransaction transaction) : IDbContextTransactionHandle
    {
        public Task CommitAsync(CancellationToken cancellationToken) => transaction.CommitAsync(cancellationToken);

        public Task RollbackAsync(CancellationToken cancellationToken) => transaction.RollbackAsync(cancellationToken);

        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}
