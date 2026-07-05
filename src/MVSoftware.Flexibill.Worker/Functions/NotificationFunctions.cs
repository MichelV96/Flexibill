using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace MVSoftware.Flexibill.Worker.Functions;

/// <summary>
/// Consumeert de "notifications"-topic (hoofdstuk 12.1, 14) en verstuurt
/// e-mailnotificaties via Azure Communication Services (IEmailSender).
/// </summary>
public class NotificationFunctions(ILogger<NotificationFunctions> logger)
{
    [Function(nameof(SendNotification))]
    public Task SendNotification(
        [ServiceBusTrigger("notifications", "email-subscriber", Connection = "ServiceBusConnection")] string message)
    {
        logger.LogInformation("Received notification message: {Message}", message);
        // TODO: IEmailSender aanroepen, rekening houdend met de notificatievoorkeur van de gebruiker (FO 4.4).
        return Task.CompletedTask;
    }
}
