using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace MVSoftware.Flexibill.Worker.Functions;

/// <summary>
/// De vier timer-triggered functions uit het Technisch Ontwerp, hoofdstuk 12.2.
/// Elke functie roept - net als de Service Bus-consumers - een Application-command aan.
/// </summary>
public class TimerFunctions(ILogger<TimerFunctions> logger)
{
    // Dagelijks om 02:00 UTC.
    [Function(nameof(DocumentRetention))]
    public Task DocumentRetention([TimerTrigger("0 0 2 * * *")] TimerInfo timer)
    {
        logger.LogInformation("Running DocumentRetentionFunction (FO 8.3)");
        // TODO: verwijder documenten ouder dan de retentietermijn (zie 11.2).
        return Task.CompletedTask;
    }

    // Dagelijks om 03:00 UTC.
    [Function(nameof(ContractExpiryAlert))]
    public Task ContractExpiryAlert([TimerTrigger("0 0 3 * * *")] TimerInfo timer)
    {
        logger.LogInformation("Running ContractExpiryAlertFunction (FO 9.3)");
        return Task.CompletedTask;
    }

    // Elk uur.
    [Function(nameof(ApprovalEscalation))]
    public Task ApprovalEscalation([TimerTrigger("0 0 * * * *")] TimerInfo timer)
    {
        logger.LogInformation("Running ApprovalEscalationFunction (FO 6.4)");
        return Task.CompletedTask;
    }

    // Dagelijks om 01:00 UTC.
    [Function(nameof(BudgetRecalculation))]
    public Task BudgetRecalculation([TimerTrigger("0 0 1 * * *")] TimerInfo timer)
    {
        logger.LogInformation("Running BudgetRecalculationFunction");
        return Task.CompletedTask;
    }
}
