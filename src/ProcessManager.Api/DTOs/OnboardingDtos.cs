using ProcessManager.Domain.Entities;

namespace ProcessManager.Api.DTOs;

/// <summary>Public signup payload — creates tenant + admin user + onboarding state in a single call.</summary>
public record PublicSignupDto(
    string CompanyName,
    string Subdomain,
    OnboardingIndustry Industry,
    string AdminUserName,
    string AdminEmail,
    string AdminPassword,
    string? AdminDisplayName);

/// <summary>Returned from the signup endpoint — the caller immediately uses the token.</summary>
public record PublicSignupResultDto(
    Guid TenantId,
    string Subdomain,
    TokenResponseDto Token);

public record OnboardingStateDto(
    Guid Id,
    OnboardingIndustry Industry,
    int CurrentStep,
    bool IsCompleted,
    bool IsSkipped,
    Guid? FirstKindId,
    Guid? FirstStepTemplateId,
    Guid? FirstProcessId,
    Guid? FirstJobId,
    DateTime? SignupAt,
    DateTime? CompletedAt,
    DateTime? SkippedAt,
    DateTime? FirstJobCompletedAt);

public record UpdateOnboardingStepDto(int CurrentStep, Guid? FirstKindId, Guid? FirstStepTemplateId, Guid? FirstProcessId, Guid? FirstJobId);

public record SkipOnboardingDto(bool SeedSample);

/// <summary>Industries offered on the welcome step — map to sample process variants.</summary>
public record OnboardingIndustryOptionDto(OnboardingIndustry Value, string Label, string Description);

public record TenantFeatureFlagsDto(
    bool ShowAdvancedModules,
    bool ShowQualityTools,
    bool ShowProductionTools,
    bool ShowWarehouseTools,
    bool ShowTrainingTools);
