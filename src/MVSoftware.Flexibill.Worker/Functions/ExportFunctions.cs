using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace MVSoftware.Flexibill.Worker.Functions;

/// <summary>
/// Consumeert de "invoice-approved" en "expense-approved" topics (hoofdstuk 12.1)
/// en verwerkt de export naar het boekhoudpakket via de betreffende
/// IAccountingConnector (hoofdstuk 13), aangeroepen als Application-command.
/// </summary>
public class ExportFunctions(ILogger<ExportFunctions> logger)
{
    [Function(nameof(ProcessInvoiceApproved))]
    public Task ProcessInvoiceApproved(
        [ServiceBusTrigger("invoice-approved", "export-subscriber", Connection = "ServiceBusConnection")] string message)
    {
        logger.LogInformation("Received invoice-approved message: {Message}", message);
        // TODO: voer ProcessInvoiceExportCommand uit via MediatR.
        return Task.CompletedTask;
    }

    [Function(nameof(ProcessExpenseApproved))]
    public Task ProcessExpenseApproved(
        [ServiceBusTrigger("expense-approved", "export-subscriber", Connection = "ServiceBusConnection")] string message)
    {
        logger.LogInformation("Received expense-approved message: {Message}", message);
        // TODO: voer ProcessExpenseExportCommand uit via MediatR.
        return Task.CompletedTask;
    }
}
