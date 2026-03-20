using ProcessManager.Api.DTOs;

namespace ProcessManager.Web.Services;

/// <summary>
/// Scoped service that holds the active domain vocabulary for the duration of a
/// Blazor Server circuit. Provides term properties with sensible defaults when
/// no vocabulary is active. Loaded once per circuit via MainLayout.
/// </summary>
public class VocabularyService
{
    private DomainVocabularyResponseDto? _active;

    /// <summary>Raised when the active vocabulary changes (load, activate, deactivate).</summary>
    public event Action? OnChange;

    /// <summary>Whether the service has successfully loaded the active vocabulary.</summary>
    public bool IsLoaded { get; private set; }

    /// <summary>Name of the active vocabulary, or null if none.</summary>
    public string? ActiveName => _active?.Name;

    // ── Singular term properties ────────────────────────────────────────────
    public string Kind      => _active?.TermKind      ?? "Kind";
    public string KindCode  => _active?.TermKindCode  ?? "Kind Code";
    public string Grade     => _active?.TermGrade     ?? "Grade";
    public string Item      => _active?.TermItem      ?? "Item";
    public string ItemId    => _active?.TermItemId    ?? "Item ID";
    public string Batch     => _active?.TermBatch     ?? "Batch";
    public string BatchId   => _active?.TermBatchId   ?? "Batch ID";
    public string Job       => _active?.TermJob       ?? "Job";
    public string Workflow  => _active?.TermWorkflow  ?? "Workflow";
    public string Process   => _active?.TermProcess   ?? "Process";
    public string Step      => _active?.TermStep      ?? "Step";
    public string Workorder => _active?.TermWorkorder ?? "Workorder";

    // ── Plural forms ────────────────────────────────────────────────────────
    public string Kinds      => Pluralize(Kind);
    public string Grades     => Pluralize(Grade);
    public string Items      => Pluralize(Item);
    public string Batches    => Pluralize(Batch);
    public string Jobs       => Pluralize(Job);
    public string Workflows  => Pluralize(Workflow);
    public string Processes  => Pluralize(Process);
    public string Steps      => Pluralize(Step);
    public string Workorders => Pluralize(Workorder);

    // ── Compound labels for nav & headings ──────────────────────────────────
    public string KindsAndGrades  => $"{Kinds} & {Grades}";
    public string StepTemplates   => $"{Step} Templates";
    public string StepExecutions  => $"{Step} Executions";

    /// <summary>
    /// Loads the active vocabulary from the API. Safe to call multiple times;
    /// only the first call fetches. Failures are silently swallowed so the UI
    /// falls back to defaults.
    /// </summary>
    public async Task LoadAsync(ApiClient api)
    {
        if (IsLoaded) return;
        try
        {
            _active = await api.GetActiveVocabularyAsync();
            IsLoaded = true;
            OnChange?.Invoke();
        }
        catch
        {
            // Best-effort — if the API is unreachable, use defaults silently.
            // IsLoaded stays false so the next circuit init can retry.
        }
    }

    /// <summary>
    /// Immediately replaces the cached vocabulary (used after activate/deactivate
    /// so the current user sees the change without a page refresh).
    /// </summary>
    public void SetVocabulary(DomainVocabularyResponseDto? vocab)
    {
        _active = vocab;
        IsLoaded = true;
        OnChange?.Invoke();
    }

    /// <summary>
    /// Simple English pluralization for domain terms.
    /// Handles common suffix rules: s/x/sh/ch → +es, consonant+y → ies, else → +s.
    /// </summary>
    public static string Pluralize(string term)
    {
        if (string.IsNullOrEmpty(term)) return term;

        if (term.EndsWith("ss", StringComparison.OrdinalIgnoreCase) ||
            term.EndsWith("x", StringComparison.OrdinalIgnoreCase) ||
            term.EndsWith("sh", StringComparison.OrdinalIgnoreCase) ||
            term.EndsWith("ch", StringComparison.OrdinalIgnoreCase))
            return term + "es";

        if (term.EndsWith("y", StringComparison.OrdinalIgnoreCase) &&
            term.Length > 1 &&
            !"aeiou".Contains(term[^2], StringComparison.OrdinalIgnoreCase))
            return term[..^1] + "ies";

        if (term.EndsWith("s", StringComparison.OrdinalIgnoreCase))
            return term;

        return term + "s";
    }
}
