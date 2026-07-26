using Stripe;

namespace ProcessManager.Api.Services;

public class StripeService : IStripeService
{
    private readonly string _webhookSecret;

    public StripeService(IConfiguration configuration)
    {
        var secretKey = configuration["Stripe:SecretKey"];
        if (!string.IsNullOrEmpty(secretKey))
            StripeConfiguration.ApiKey = secretKey;

        _webhookSecret = configuration["Stripe:WebhookSecret"] ?? string.Empty;
    }

    public async Task<StripeCustomerResult> CreateCustomerAsync(string name, string email, Guid tenantId)
    {
        var service = new CustomerService();
        var customer = await service.CreateAsync(new CustomerCreateOptions
        {
            Name = name,
            Email = email,
            Metadata = new Dictionary<string, string> { ["tenant_id"] = tenantId.ToString() }
        });
        return new StripeCustomerResult(customer.Id);
    }

    public async Task<StripeSubscriptionResult> CreateTrialSubscriptionAsync(string customerId, string priceId, int trialDays)
    {
        var service = new SubscriptionService();
        var subscription = await service.CreateAsync(new SubscriptionCreateOptions
        {
            Customer = customerId,
            Items = new List<SubscriptionItemOptions> { new() { Price = priceId } },
            TrialPeriodDays = trialDays
        });

        return new StripeSubscriptionResult(
            subscription.Id,
            subscription.TrialEnd ?? DateTime.UtcNow.AddDays(trialDays),
            subscription.CurrentPeriodEnd);
    }

    public async Task<string> CreateBillingPortalSessionAsync(string customerId, string returnUrl)
    {
        var service = new Stripe.BillingPortal.SessionService();
        var session = await service.CreateAsync(new Stripe.BillingPortal.SessionCreateOptions
        {
            Customer = customerId,
            ReturnUrl = returnUrl
        });
        return session.Url;
    }

    public async Task CancelSubscriptionAsync(string subscriptionId)
    {
        var service = new SubscriptionService();
        await service.CancelAsync(subscriptionId);
    }

    public bool VerifyWebhookSignature(string payload, string signature, out string? eventId, out string? eventType, out string? rawEvent)
    {
        try
        {
            var stripeEvent = EventUtility.ConstructEvent(payload, signature, _webhookSecret);
            eventId = stripeEvent.Id;
            eventType = stripeEvent.Type;
            rawEvent = payload.Length > 2000 ? payload[..2000] : payload;
            return true;
        }
        catch
        {
            eventId = null;
            eventType = null;
            rawEvent = null;
            return false;
        }
    }

    public async Task<StripeCheckoutSessionResult> CreateCheckoutSessionAsync(
        string customerId, string priceId, string successUrl, string cancelUrl, string? couponCode)
    {
        var options = new Stripe.Checkout.SessionCreateOptions
        {
            Customer = customerId,
            Mode = "subscription",
            LineItems = new List<Stripe.Checkout.SessionLineItemOptions>
            {
                new() { Price = priceId, Quantity = 1 }
            },
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl
        };

        if (!string.IsNullOrEmpty(couponCode))
        {
            options.Discounts = new List<Stripe.Checkout.SessionDiscountOptions>
            {
                new() { Coupon = couponCode }
            };
        }

        var service = new Stripe.Checkout.SessionService();
        var session = await service.CreateAsync(options);
        return new StripeCheckoutSessionResult(session.Id, session.Url);
    }

    public async Task<StripeSubscriptionResult> UpdateSubscriptionPlanAsync(
        string subscriptionId, string newPriceId, bool atPeriodEnd)
    {
        var service = new SubscriptionService();
        var subscription = await service.GetAsync(subscriptionId);
        var itemId = subscription.Items.Data[0].Id;

        if (atPeriodEnd)
        {
            var scheduleService = new SubscriptionScheduleService();
            var schedule = await scheduleService.CreateAsync(new SubscriptionScheduleCreateOptions
            {
                FromSubscription = subscriptionId
            });

            await scheduleService.UpdateAsync(schedule.Id, new SubscriptionScheduleUpdateOptions
            {
                Phases = new List<SubscriptionSchedulePhaseOptions>
                {
                    new()
                    {
                        Items = new List<SubscriptionSchedulePhaseItemOptions>
                        {
                            new() { Price = newPriceId, Quantity = 1 }
                        },
                        StartDate = subscription.CurrentPeriodEnd
                    }
                }
            });

            return new StripeSubscriptionResult(subscriptionId, subscription.TrialEnd ?? DateTime.UtcNow, subscription.CurrentPeriodEnd);
        }

        var updated = await service.UpdateAsync(subscriptionId, new SubscriptionUpdateOptions
        {
            Items = new List<SubscriptionItemOptions>
            {
                new() { Id = itemId, Price = newPriceId }
            },
            ProrationBehavior = "create_prorations"
        });

        return new StripeSubscriptionResult(updated.Id, updated.TrialEnd ?? DateTime.UtcNow, updated.CurrentPeriodEnd);
    }
}
