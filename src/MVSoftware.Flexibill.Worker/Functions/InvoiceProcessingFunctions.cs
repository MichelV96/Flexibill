using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace MVSoftware.Flexibill.Worker.Functions;

/// <summary>
/// Consumeert de "invoice-ocr-requested"-queue (Technisch Ontwerp, hoofdstuk 12.1):
/// roept Azure AI Document Intelligence aan en voert daarna zelf, via dezelfde
/// Application-laag als de Web App, het ProcessOcrResultCommand uit (hoofdstuk 10.3).
///
/// TODO (volgende stap): IOcrService en ISender (MediatR) injecteren zodra de
/// Application-laag commands bevat.
/// </summary>
public class InvoiceProcessingFunctions(ILogger<InvoiceProcessingFunctions> logger)
{
    [Function(nameof(ProcessInvoiceOcrRequested))]
    public Task ProcessInvoiceOcrRequested(
        [ServiceBusTrigger("invoice-ocr-requested", Connection = "ServiceBusConnection")] string message)
    {
        logger.LogInformation("Received invoice-ocr-requested message: {Message}", message);
        // TODO: roep IOcrService aan en voer ProcessOcrResultCommand uit via MediatR.
        return Task.CompletedTask;
    }
}
