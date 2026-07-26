namespace ProcessManager.Domain.Enums;

public enum OutOfControlRule
{
    Rule1_BeyondThreeSigma,
    Rule2_TwoOfThreeBeyondTwoSigma,
    Rule3_FourOfFiveBeyondOneSigma,
    Rule4_EightConsecutiveOneSide
}
