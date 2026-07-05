using MVSoftware.Flexibill.Application.Common.Interfaces;

namespace MVSoftware.Flexibill.Infrastructure;

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
