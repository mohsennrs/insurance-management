using shared_messaging;
using shared_messaging.Events;

namespace document_management_service.EventHandlers;

/// <summary>
/// Handles ClaimCreatedEvent from Claims Service
/// </summary>
public class ClaimCreatedEventHandler : IEventHandler<ClaimCreatedEvent>
{
    private readonly ILogger<ClaimCreatedEventHandler> _logger;

    public ClaimCreatedEventHandler(ILogger<ClaimCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(ClaimCreatedEvent @event)
    {
        _logger.LogInformation(
            "📥 Received ClaimCreatedEvent: Claim {ClaimNumber} (ID: {ClaimId}) created for policy {PolicyNumber}",
            @event.ClaimNumber,
            @event.ClaimId,
            @event.PolicyNumber);

        _logger.LogInformation(
            "   Claimant: {ClaimantName} ({ClaimantEmail}), Amount: ${ClaimAmount:N2}",
            @event.ClaimantName,
            @event.ClaimantEmail,
            @event.ClaimAmount);

        // TODO: You can add business logic here, for example:
        // - Prepare document storage folder structure
        // - Send email notification to claimant about document upload instructions
        // - Initialize document checklist for this claim type
        // - Create default folders in MinIO for this claim

        _logger.LogInformation(
            "   Documents Service is ready to accept uploads for claim {ClaimId}",
            @event.ClaimId);

        await Task.CompletedTask;
    }
}