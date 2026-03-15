namespace ProcessManager.Domain.Entities;

/// <summary>
/// Attaches a run chart to a step template, sourcing data from any numeric prompt
/// anywhere in the system (same step, different step, or different process).
/// </summary>
public class RunChartWidget : BaseEntity
{
    /// <summary>The step template on which this chart is displayed to operators.</summary>
    public Guid StepTemplateId { get; set; }

    /// <summary>
    /// The StepTemplateContent record (PromptType = NumericEntry) whose PromptResponse
    /// history is charted. May point to a prompt on a completely different step template.
    /// </summary>
    public Guid SourceContentId { get; set; }

    /// <summary>Display label shown above the chart (defaults to the source prompt label).</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>How many recent data points to include in the rolling window (default 30).</summary>
    public int ChartWindowSize { get; set; } = 30;

    /// <summary>
    /// Override lower spec limit. When null, the chart inherits the source prompt's MinValue.
    /// </summary>
    public decimal? SpecMin { get; set; }

    /// <summary>
    /// Override upper spec limit. When null, the chart inherits the source prompt's MaxValue.
    /// </summary>
    public decimal? SpecMax { get; set; }

    /// <summary>Display order among the step template's run charts (0-based, gaps allowed).</summary>
    public int DisplayOrder { get; set; }

    // Navigation
    public StepTemplate StepTemplate { get; set; } = null!;
    public StepTemplateContent SourceContent { get; set; } = null!;
}
