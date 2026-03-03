using ProcessManager.Api.DTOs;
using ProcessManager.Domain.Entities;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Api.Services;

/// <summary>
/// Evaluates the maturity of a StepTemplate against the default Phase 8b rule set.
/// Rules are evaluated in-process; results are transient (not persisted).
/// </summary>
public static class MaturityScoringService
{
    // ────────────────────────────────────────────────────────────────────────────
    // Public entry point
    // ────────────────────────────────────────────────────────────────────────────

    public static MaturityReportDto Evaluate(StepTemplate step)
    {
        var contents = step.Contents.ToList();
        var results = new List<MaturityRuleResultDto>();

        // R01 — Step has at least one content block of any kind (Error)
        results.Add(Eval("R01",
            "Step has at least one content block",
            MaturityRuleOutcome.Fail,
            contents.Count > 0,
            "Add at least one text, image, or prompt block via the Work Instructions section."));

        // R02 — Step has at least one Setup content block (Warning)
        results.Add(Eval("R02",
            "Step has at least one Setup block",
            MaturityRuleOutcome.Warn,
            contents.Any(c => c.ContentCategory == ContentCategory.Setup),
            "Add a Setup block describing what to prepare before work begins."));

        // R03 — Step has at least one Safety content block (Warning)
        results.Add(Eval("R03",
            "Step has at least one Safety block",
            MaturityRuleOutcome.Warn,
            contents.Any(c => c.ContentCategory == ContentCategory.Safety),
            "Add a Safety block describing hazards, PPE requirements, or stop conditions."));

        // R04 — Every Safety block has AcknowledgmentRequired = true (Error)
        var safetyBlocks = contents.Where(c => c.ContentCategory == ContentCategory.Safety).ToList();
        var allSafetyBlocksRequireAck = safetyBlocks.Count == 0
            || safetyBlocks.All(c => c.AcknowledgmentRequired);
        results.Add(Eval("R04",
            "Every Safety block requires acknowledgment",
            MaturityRuleOutcome.Fail,
            allSafetyBlocksRequireAck,
            "Enable AcknowledgmentRequired on all Safety blocks so operators must explicitly confirm each hazard."));

        // R05 — Step that has Inspection blocks has at least one PassFail or NumericEntry prompt (Error)
        var hasInspectionBlocks = contents.Any(c => c.ContentCategory == ContentCategory.Inspection);
        if (hasInspectionBlocks)
        {
            var hasDataPrompt = contents.Any(c =>
                c.ContentType == StepContentType.Prompt &&
                (c.PromptType == PromptType.PassFail || c.PromptType == PromptType.NumericEntry));

            results.Add(Eval("R05",
                "Inspection blocks paired with a data-collection prompt",
                MaturityRuleOutcome.Fail,
                hasDataPrompt,
                "Add a PassFail or NumericEntry prompt alongside each Inspection content block."));
        }

        // R06 — Every NumericEntry prompt has MinValue and MaxValue (Warning)
        var numericPrompts = contents
            .Where(c => c.ContentType == StepContentType.Prompt && c.PromptType == PromptType.NumericEntry)
            .ToList();

        var allNumericHaveLimits = numericPrompts.Count == 0
            || numericPrompts.All(c => c.MinValue.HasValue && c.MaxValue.HasValue);
        results.Add(Eval("R06",
            "Every NumericEntry prompt has LSL and USL",
            MaturityRuleOutcome.Warn,
            allNumericHaveLimits,
            "Set MinValue (LSL) and MaxValue (USL) on all NumericEntry prompts."));

        // R07 — Every hard-limit NumericEntry prompt has NominalValue (Warning)
        var hardLimitPrompts = numericPrompts.Where(c => c.IsHardLimit).ToList();
        var allHardLimitHaveNominal = hardLimitPrompts.Count == 0
            || hardLimitPrompts.All(c => c.NominalValue.HasValue);
        results.Add(Eval("R07",
            "Every hard-limit NumericEntry prompt has a Nominal value",
            MaturityRuleOutcome.Warn,
            allHardLimitHaveNominal,
            "Set a NominalValue on all NumericEntry prompts where IsHardLimit is enabled."));

        // R08 — No uncategorised blocks (ContentCategory is null) (Warning — legacy data)
        var allBlocksCategorised = contents.Count == 0
            || contents.All(c => c.ContentCategory.HasValue);
        results.Add(Eval("R08",
            "All content blocks have a category assigned",
            MaturityRuleOutcome.Warn,
            allBlocksCategorised,
            "Assign a ContentCategory (Setup / Safety / Inspection / Reference / Note) to every block."));

        // ────────────────────────────────────────────────────────────────────────
        // Compute score
        // ────────────────────────────────────────────────────────────────────────

        int applicable = results.Count;
        int passing = results.Count(r => r.Outcome == MaturityRuleOutcome.Pass);
        bool hasErrors = results.Any(r => r.Outcome == MaturityRuleOutcome.Fail);

        int rawScore = applicable > 0
            ? (int)Math.Round((double)passing / applicable * 100)
            : 0;

        // Error-level failures cap the score at 79 (Developing ceiling)
        int score = hasErrors ? Math.Min(rawScore, 79) : rawScore;

        var level = score switch
        {
            100 => MaturityLevel.Optimised,
            >= 80 => MaturityLevel.Defined,
            >= 50 => MaturityLevel.Developing,
            _ => MaturityLevel.Draft
        };

        return new MaturityReportDto(
            step.Id, step.Code, step.Name,
            score, level, hasErrors,
            results);
    }

    public static MaturitySummaryDto Summarise(StepTemplate step)
    {
        var report = Evaluate(step);
        return new MaturitySummaryDto(report.Score, report.Level, report.HasBlockingErrors);
    }

    // ────────────────────────────────────────────────────────────────────────────
    // Helpers
    // ────────────────────────────────────────────────────────────────────────────

    private static MaturityRuleResultDto Eval(
        string ruleId,
        string description,
        MaturityRuleOutcome failOutcome,
        bool passes,
        string? remediationHint) => new(
            ruleId,
            description,
            passes ? MaturityRuleOutcome.Pass : failOutcome,
            passes ? null : remediationHint);
}
