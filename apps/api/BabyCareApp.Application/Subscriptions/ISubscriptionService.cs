namespace BabyCareApp.Application.Subscriptions;

public interface ISubscriptionService
{
    /// <summary>Starts a Trialing subscription for the resolved tenant. Fails if the tenant
    /// already has a Trialing/Active/PastDue subscription — cancel or let it expire first.</summary>
    Task<SubscriptionResponse> SubscribeAsync(SubscribeRequest request, CancellationToken cancellationToken = default);

    /// <summary>Lazily transitions an expired trial to Expired on read — see the
    /// implementation note on why this isn't a background job.</summary>
    Task<SubscriptionResponse> GetCurrentSubscriptionAsync(CancellationToken cancellationToken = default);

    Task<SubscriptionResponse> ChangePlanAsync(ChangePlanRequest request, CancellationToken cancellationToken = default);

    Task CancelSubscriptionAsync(CancellationToken cancellationToken = default);

    /// <summary>Null means no Trialing/Active/PastDue subscription is in effect for this
    /// tenant — callers (e.g. branch creation) should treat that as unlimited, preserving
    /// pre-Slice-7 behavior for tenants that haven't subscribed yet, rather than silently
    /// blocking an action no one asked to gate.</summary>
    Task<SubscriptionPlanLimits?> GetEffectiveLimitsAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Billing-integration preparation — not called by any real payment gateway yet (none
    /// is integrated). Exposed today only via a Super-Admin "confirm payment" action, for
    /// manual/EFT payments, which are common in South Africa alongside card gateways.
    /// </summary>
    Task RecordPaymentSucceededAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task RecordPaymentFailedAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
