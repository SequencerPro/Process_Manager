using ProcessManager.Api.Services;

namespace ProcessManager.Tests;

public class TestStripeService : IStripeService
{
    public bool VerifySignature { get; set; } = true;
    public string NextEventId { get; set; } = "evt_test_123";
    public string NextEventType { get; set; } = "invoice.payment_succeeded";

    public Task<StripeCustomerResult> CreateCustomerAsync(string name, string email, Guid tenantId)
        => Task.FromResult(new StripeCustomerResult($"cus_test_{tenantId.ToString()[..8]}"));

    public Task<StripeSubscriptionResult> CreateTrialSubscriptionAsync(string customerId, string priceId, int trialDays)
        => Task.FromResult(new StripeSubscriptionResult(
            $"sub_test_{Guid.NewGuid().ToString()[..8]}",
            DateTime.UtcNow.AddDays(trialDays),
            DateTime.UtcNow.AddDays(trialDays)));

    public Task<string> CreateBillingPortalSessionAsync(string customerId, string returnUrl)
        => Task.FromResult($"https://billing.stripe.com/test-session?customer={customerId}&return={returnUrl}");

    public Task CancelSubscriptionAsync(string subscriptionId)
        => Task.CompletedTask;

    public bool VerifyWebhookSignature(string payload, string signature, out string? eventId, out string? eventType, out string? rawEvent)
    {
        if (!VerifySignature)
        {
            eventId = null;
            eventType = null;
            rawEvent = null;
            return false;
        }

        eventId = NextEventId;
        eventType = NextEventType;
        rawEvent = payload;
        return true;
    }

    public Task<StripeCheckoutSessionResult> CreateCheckoutSessionAsync(
        string customerId, string priceId, string successUrl, string cancelUrl, string? couponCode)
        => Task.FromResult(new StripeCheckoutSessionResult(
            $"cs_test_{Guid.NewGuid().ToString()[..8]}",
            $"https://checkout.stripe.com/test-session?customer={customerId}"));

    public Task<StripeSubscriptionResult> UpdateSubscriptionPlanAsync(
        string subscriptionId, string newPriceId, bool atPeriodEnd)
        => Task.FromResult(new StripeSubscriptionResult(
            subscriptionId,
            DateTime.UtcNow,
            DateTime.UtcNow.AddMonths(1)));
}
