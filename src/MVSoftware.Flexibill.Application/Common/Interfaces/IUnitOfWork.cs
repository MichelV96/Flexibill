namespace MVSoftware.Flexibill.Application.Common.Interfaces;

/// <summary>
/// Laat <see cref="Behaviors.TransactionBehavior{TRequest,TResponse}"/> een EF Core-transactie
/// openen zonder dat Application een dependency op Infrastructure/EF Core krijgt (Technisch
/// Ontwerp, hoofdstuk 3.1/6.3 punt 4). Infrastructure implementeert dit bovenop de DbContext.
/// </summary>
public interface IUnitOfWork
{
    Task<IDbContextTransactionHandle> BeginTransactionAsync(CancellationToken cancellationToken);
}

/// <summary>Een lopende databasetransactie; committen/rollbacken gebeurt expliciet door de aanroeper.</summary>
public interface IDbContextTransactionHandle : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken);
    Task RollbackAsync(CancellationToken cancellationToken);
}
