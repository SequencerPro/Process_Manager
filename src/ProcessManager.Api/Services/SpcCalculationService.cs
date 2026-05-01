using ProcessManager.Api.DTOs;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Api.Services;

public interface ISpcCalculationService
{
    SpcCalculationResultDto Calculate(List<decimal> values, int subgroupSize, decimal? lsl, decimal? usl);
    List<SpcOutOfControlPointDto> DetectOutOfControl(List<SpcSubgroupDto> subgroups, decimal cl, decimal stdDev);
}

public class SpcCalculationService : ISpcCalculationService
{
    // A2 constants for X-bar chart UCL/LCL (indexed by subgroup size 2..10)
    private static readonly Dictionary<int, decimal> A2 = new()
    {
        { 2, 1.880m }, { 3, 1.023m }, { 4, 0.729m }, { 5, 0.577m },
        { 6, 0.483m }, { 7, 0.419m }, { 8, 0.373m }, { 9, 0.337m }, { 10, 0.308m }
    };

    // D3 constants for R chart LCL (indexed by subgroup size 2..10)
    private static readonly Dictionary<int, decimal> D3 = new()
    {
        { 2, 0m }, { 3, 0m }, { 4, 0m }, { 5, 0m },
        { 6, 0m }, { 7, 0.076m }, { 8, 0.136m }, { 9, 0.184m }, { 10, 0.223m }
    };

    // D4 constants for R chart UCL (indexed by subgroup size 2..10)
    private static readonly Dictionary<int, decimal> D4 = new()
    {
        { 2, 3.267m }, { 3, 2.575m }, { 4, 2.282m }, { 5, 2.115m },
        { 6, 2.004m }, { 7, 1.924m }, { 8, 1.864m }, { 9, 1.816m }, { 10, 1.777m }
    };

    // d2 constants for estimating sigma from R-bar (indexed by subgroup size 2..10)
    private static readonly Dictionary<int, decimal> d2 = new()
    {
        { 2, 1.128m }, { 3, 1.693m }, { 4, 2.059m }, { 5, 2.326m },
        { 6, 2.534m }, { 7, 2.704m }, { 8, 2.847m }, { 9, 2.970m }, { 10, 3.078m }
    };

    public SpcCalculationResultDto Calculate(List<decimal> values, int subgroupSize, decimal? lsl, decimal? usl)
    {
        if (values.Count == 0)
        {
            return new SpcCalculationResultDto(0, 0, 0, 0, 0, 0, 0, 0, null, null, null, null, 0, 0, 0,
                new List<SpcOutOfControlPointDto>());
        }

        var clampedSize = Math.Clamp(subgroupSize, 2, 10);
        var subgroups = BuildSubgroups(values, clampedSize);

        if (subgroups.Count == 0)
        {
            return new SpcCalculationResultDto(0, 0, 0, 0, 0, 0, 0, 0, null, null, null, null, 0, 0, values.Count,
                new List<SpcOutOfControlPointDto>());
        }

        var xBar = subgroups.Average(s => s.Mean);
        var rBar = subgroups.Average(s => s.Range);

        var a2 = A2[clampedSize];
        var d3 = D3[clampedSize];
        var d4 = D4[clampedSize];
        var d2Val = d2[clampedSize];

        // X-bar chart limits
        var ucl = xBar + a2 * rBar;
        var lcl = xBar - a2 * rBar;
        var cl = xBar;

        // R chart limits
        var rangeUcl = d4 * rBar;
        var rangeLcl = d3 * rBar;
        var rangeCl = rBar;

        // Estimated standard deviation (within-subgroup)
        var sigmaWithin = d2Val > 0 ? rBar / d2Val : 0;

        // Overall standard deviation
        var overallStdDev = values.Count > 1
            ? (decimal)Math.Sqrt((double)values.Select(v => (v - xBar) * (v - xBar)).Sum() / (values.Count - 1))
            : 0;

        // Capability indices (require spec limits)
        decimal? cp = null, cpk = null, pp = null, ppk = null;

        if (lsl.HasValue && usl.HasValue && sigmaWithin > 0)
        {
            cp = (usl.Value - lsl.Value) / (6 * sigmaWithin);
            var cpuWithin = (usl.Value - xBar) / (3 * sigmaWithin);
            var cplWithin = (xBar - lsl.Value) / (3 * sigmaWithin);
            cpk = Math.Min(cpuWithin, cplWithin);
        }
        else if (lsl.HasValue && sigmaWithin > 0)
        {
            cpk = (xBar - lsl.Value) / (3 * sigmaWithin);
        }
        else if (usl.HasValue && sigmaWithin > 0)
        {
            cpk = (usl.Value - xBar) / (3 * sigmaWithin);
        }

        if (lsl.HasValue && usl.HasValue && overallStdDev > 0)
        {
            pp = (usl.Value - lsl.Value) / (6 * overallStdDev);
            var ppuOverall = (usl.Value - xBar) / (3 * overallStdDev);
            var pplOverall = (xBar - lsl.Value) / (3 * overallStdDev);
            ppk = Math.Min(ppuOverall, pplOverall);
        }

        var outOfControl = DetectOutOfControl(subgroups, cl, sigmaWithin);

        return new SpcCalculationResultDto(
            XBar: Math.Round(xBar, 6),
            RBar: Math.Round(rBar, 6),
            UCL: Math.Round(ucl, 6),
            LCL: Math.Round(lcl, 6),
            CL: Math.Round(cl, 6),
            RangeUCL: Math.Round(rangeUcl, 6),
            RangeLCL: Math.Round(rangeLcl, 6),
            RangeCL: Math.Round(rangeCl, 6),
            Cp: cp.HasValue ? Math.Round(cp.Value, 4) : null,
            Cpk: cpk.HasValue ? Math.Round(cpk.Value, 4) : null,
            Pp: pp.HasValue ? Math.Round(pp.Value, 4) : null,
            Ppk: ppk.HasValue ? Math.Round(ppk.Value, 4) : null,
            StdDev: Math.Round(sigmaWithin, 6),
            SubgroupCount: subgroups.Count,
            TotalPoints: values.Count,
            OutOfControlPoints: outOfControl
        );
    }

    public List<SpcOutOfControlPointDto> DetectOutOfControl(List<SpcSubgroupDto> subgroups, decimal cl, decimal stdDev)
    {
        var violations = new List<SpcOutOfControlPointDto>();
        if (subgroups.Count == 0 || stdDev <= 0) return violations;

        var sigma1 = stdDev;
        var sigma2 = 2 * stdDev;
        var sigma3 = 3 * stdDev;

        for (int i = 0; i < subgroups.Count; i++)
        {
            var sg = subgroups[i];
            var deviation = Math.Abs(sg.Mean - cl);

            // Rule 1: 1 point beyond 3 sigma
            if (deviation > sigma3)
            {
                violations.Add(new SpcOutOfControlPointDto(
                    sg.Index, sg.Mean,
                    nameof(OutOfControlRule.Rule1_BeyondThreeSigma),
                    $"Subgroup {sg.Index} mean {sg.Mean:F4} is beyond 3σ from center line"));
            }

            // Rule 2: 2 of 3 consecutive points beyond 2 sigma on the same side
            if (i >= 2)
            {
                var recent3 = subgroups.Skip(i - 2).Take(3).ToList();
                var above2 = recent3.Count(s => s.Mean > cl + sigma2);
                var below2 = recent3.Count(s => s.Mean < cl - sigma2);
                if (above2 >= 2 || below2 >= 2)
                {
                    if (!violations.Any(v => v.SubgroupIndex == sg.Index && v.Rule == nameof(OutOfControlRule.Rule2_TwoOfThreeBeyondTwoSigma)))
                    {
                        violations.Add(new SpcOutOfControlPointDto(
                            sg.Index, sg.Mean,
                            nameof(OutOfControlRule.Rule2_TwoOfThreeBeyondTwoSigma),
                            $"2 of 3 consecutive points beyond 2σ at subgroup {sg.Index}"));
                    }
                }
            }

            // Rule 3: 4 of 5 consecutive points beyond 1 sigma on the same side
            if (i >= 4)
            {
                var recent5 = subgroups.Skip(i - 4).Take(5).ToList();
                var above1 = recent5.Count(s => s.Mean > cl + sigma1);
                var below1 = recent5.Count(s => s.Mean < cl - sigma1);
                if (above1 >= 4 || below1 >= 4)
                {
                    if (!violations.Any(v => v.SubgroupIndex == sg.Index && v.Rule == nameof(OutOfControlRule.Rule3_FourOfFiveBeyondOneSigma)))
                    {
                        violations.Add(new SpcOutOfControlPointDto(
                            sg.Index, sg.Mean,
                            nameof(OutOfControlRule.Rule3_FourOfFiveBeyondOneSigma),
                            $"4 of 5 consecutive points beyond 1σ at subgroup {sg.Index}"));
                    }
                }
            }

            // Rule 4: 8 consecutive points on the same side of center line
            if (i >= 7)
            {
                var recent8 = subgroups.Skip(i - 7).Take(8).ToList();
                var allAbove = recent8.All(s => s.Mean > cl);
                var allBelow = recent8.All(s => s.Mean < cl);
                if (allAbove || allBelow)
                {
                    if (!violations.Any(v => v.SubgroupIndex == sg.Index && v.Rule == nameof(OutOfControlRule.Rule4_EightConsecutiveOneSide)))
                    {
                        violations.Add(new SpcOutOfControlPointDto(
                            sg.Index, sg.Mean,
                            nameof(OutOfControlRule.Rule4_EightConsecutiveOneSide),
                            $"8 consecutive points on same side of center line at subgroup {sg.Index}"));
                    }
                }
            }
        }

        return violations;
    }

    public static List<SpcSubgroupDto> BuildSubgroups(List<decimal> values, int subgroupSize)
    {
        var subgroups = new List<SpcSubgroupDto>();
        int index = 0;
        for (int i = 0; i + subgroupSize <= values.Count; i += subgroupSize)
        {
            var group = values.Skip(i).Take(subgroupSize).ToList();
            subgroups.Add(new SpcSubgroupDto(
                Index: index++,
                Mean: group.Average(),
                Range: group.Max() - group.Min(),
                Values: group
            ));
        }
        return subgroups;
    }
}
