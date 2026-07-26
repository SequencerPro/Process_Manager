namespace ProcessManager.Api.Services;

public interface IStripeService
{
    Task<StripeCustomerResult> CreateCustomerAsync(string name, string email, Guid tenantId);
    Task<StripeSubscriptionResult> CreateTrialSubscriptionAsync(string customerId, string priceId, int trialDays);
    Task<string> CreateBillingPortalSessionAsync(string customerId, string returnUrl);
    Task CancelSubscriptionAsync(string subscriptionId);
    bool VerifyWebhookSignature(string payload, string signature, out string? eventId, out string? eventType, out string? rawEvent);
    Task<StripeCheckoutSessionResult> CreateCheckoutSessionAsync(string customerId, string priceId, string successUrl, string cancelUrl, string? couponCode);
    Task<StripeSubscriptionResult> UpdateSubscriptionPlanAsync(string subscriptionId, string newPriceId, bool atPeriodEnd);
}

public record StripeCustomerResult(string CustomerId);
public record StripeSubscriptionResult(string SubscriptionId, DateTime TrialEnd, DateTime CurrentPeriodEnd);
public record StripeCheckoutSessionResult(string SessionId, string Url);
