namespace MVSoftware.Flexibill.Application.Common.Interfaces;

/// <summary>Maakt DateTimeOffset.UtcNow injecteerbaar/testbaar (Technisch Ontwerp, hoofdstuk 6.4).</summary>
public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}
