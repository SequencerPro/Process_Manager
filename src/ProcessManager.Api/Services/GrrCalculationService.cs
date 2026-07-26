using ProcessManager.Api.DTOs;

namespace ProcessManager.Api.Services;

public static class GrrCalculationService
{
    // d₂ constants for converting range to standard deviation (subgroup sizes 2–10)
    private static readonly Dictionary<int, double> D2 = new()
    {
        { 2, 1.128 }, { 3, 1.693 }, { 4, 2.059 }, { 5, 2.326 },
        { 6, 2.534 }, { 7, 2.704 }, { 8, 2.847 }, { 9, 2.970 }, { 10, 3.078 }
    };

    // K1 constants (number of trials) for EV computation
    private static readonly Dictionary<int, double> K1 = new()
    {
        { 2, 0.8862 }, { 3, 0.5908 }, { 4, 0.4857 }, { 5, 0.4299 },
        { 6, 0.3946 }, { 7, 0.3698 }, { 8, 0.3497 }, { 9, 0.3332 }, { 10, 0.3195 }
    };

    // K2 constants (number of operators) for AV computation
    private static readonly Dictionary<int, double> K2 = new()
    {
        { 2, 0.7071 }, { 3, 0.5231 }, { 4, 0.4467 }, { 5, 0.4030 },
        { 6, 0.3742 }, { 7, 0.3534 }, { 8, 0.3375 }, { 9, 0.3249 }, { 10, 0.3146 }
    };

    // K3 constants (number of parts) for PV computation
    private static readonly Dictionary<int, double> K3 = new()
    {
        { 2, 0.7071 }, { 3, 0.5231 }, { 4, 0.4467 }, { 5, 0.4030 },
        { 6, 0.3742 }, { 7, 0.3534 }, { 8, 0.3375 }, { 9, 0.3249 }, { 10, 0.3146 }
    };

    public static GrrCalculationResultDto? Calculate(
        List<(int Part, string Operator, int Trial, decimal Value)> measurements,
        int numberOfParts, int numberOfOperators, int numberOfTrials,
        decimal? tolerance)
    {
        if (measurements.Count < numberOfParts * numberOfOperators * numberOfTrials)
            return null;

        if (!K1.ContainsKey(numberOfTrials) || !K2.ContainsKey(numberOfOperators) || !K3.ContainsKey(numberOfParts))
            return null;

        var operators = measurements.Select(m => m.Operator).Distinct().ToList();
        var parts = measurements.Select(m => m.Part).Distinct().ToList();

        // Compute range for each operator-part combination (range across trials)
        var ranges = new List<double>();
        foreach (var op in operators)
        {
            foreach (var part in parts)
            {
                var trials = measurements
                    .Where(m => m.Operator == op && m.Part == part)
                    .Select(m => (double)m.Value)
                    .ToList();
                if (trials.Count >= 2)
                    ranges.Add(trials.Max() - trials.Min());
            }
        }

        if (!ranges.Any()) return null;

        double rBar = ranges.Average();

        // EV (Repeatability) = R̄ × K1
        double k1 = K1[numberOfTrials];
        double ev = rBar * k1;

        // Compute operator averages
        var operatorAverages = operators.Select(op =>
            measurements.Where(m => m.Operator == op).Average(m => (double)m.Value)
        ).ToList();

        double xDiff = operatorAverages.Max() - operatorAverages.Min();

        // AV (Reproducibility) = √((X̄diff × K2)² - (EV² / (n × r)))
        double k2 = K2[numberOfOperators];
        double avSquared = (xDiff * k2) * (xDiff * k2) - (ev * ev) / (numberOfParts * numberOfTrials);
        double av = avSquared > 0 ? Math.Sqrt(avSquared) : 0;

        // GRR = √(EV² + AV²)
        double grr = Math.Sqrt(ev * ev + av * av);

        // PV (Part Variation) = Rp × K3
        var partAverages = parts.Select(p =>
            measurements.Where(m => m.Part == p).Average(m => (double)m.Value)
        ).ToList();

        double rp = partAverages.Max() - partAverages.Min();
        double k3 = K3[numberOfParts];
        double pv = rp * k3;

        // TV (Total Variation) = √(GRR² + PV²)
        double tv = Math.Sqrt(grr * grr + pv * pv);

        // Percent contributions
        double percentEV = tv > 0 ? (ev / tv) * 100 : 0;
        double percentAV = tv > 0 ? (av / tv) * 100 : 0;
        double percentGRR = tv > 0 ? (grr / tv) * 100 : 0;
        double percentPV = tv > 0 ? (pv / tv) * 100 : 0;

        // Number of distinct categories (ndc)
        int ndc = (int)(1.41 * (pv / grr));
        if (ndc < 1) ndc = 1;

        // %GRR of Tolerance
        double? percentTolerance = null;
        if (tolerance.HasValue && tolerance.Value > 0)
        {
            percentTolerance = (grr / (double)tolerance.Value) * 100;
        }

        // Assessment based on %GRR of TV
        string assessment = percentGRR switch
        {
            < 10 => "Acceptable",
            < 30 => "Marginal",
            _ => "Unacceptable"
        };

        return new GrrCalculationResultDto(
            RepeatabilityEV: Math.Round((decimal)ev, 6),
            ReproducibilityAV: Math.Round((decimal)av, 6),
            GRR: Math.Round((decimal)grr, 6),
            PartVariationPV: Math.Round((decimal)pv, 6),
            TotalVariationTV: Math.Round((decimal)tv, 6),
            PercentEV: Math.Round((decimal)percentEV, 2),
            PercentAV: Math.Round((decimal)percentAV, 2),
            PercentGRR: Math.Round((decimal)percentGRR, 2),
            PercentPV: Math.Round((decimal)percentPV, 2),
            Ndc: ndc,
            PercentTolerance: percentTolerance.HasValue ? Math.Round((decimal)percentTolerance.Value, 2) : null,
            Assessment: assessment);
    }
}
