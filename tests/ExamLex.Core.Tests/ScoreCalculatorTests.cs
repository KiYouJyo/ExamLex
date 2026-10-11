using ExamLex.Core;
using Xunit;

namespace ExamLex.Core.Tests;

public class ScoreCalculatorTests
{
    [Fact]
    public void Toefl_RoundsAtFivePointTwoFive() =>
        Assert.Equal(5.5m, ScoreCalculator.Calculate(ExamKind.Toefl2026, 5m, 5m, 5.5m, 5.5m).Overall);

    [Fact]
    public void Toefl_RoundsFivePointOneTwoFiveDown() =>
        Assert.Equal(5m, ScoreCalculator.Calculate(ExamKind.Toefl2026, 5m, 5m, 5m, 5.5m).Overall);

    [Fact]
    public void Toefl_SixIsC2() =>
        Assert.Equal("C2", ScoreCalculator.Calculate(ExamKind.Toefl2026, 6m, 6m, 6m, 6m).Cefr);

    [Fact]
    public void Toefl_UsesReferenceRange() =>
        Assert.Contains("95–106", ScoreCalculator.Calculate(ExamKind.Toefl2026, 5m, 5m, 5m, 5m).Comparison);

    [Fact]
    public void Ielts_QuarterGoesUp() =>
        Assert.Equal(6.5m, ScoreCalculator.Calculate(ExamKind.Ielts, 6.5m, 6.5m, 5m, 7m).Overall);

    [Fact]
    public void Ielts_OneEighthGoesDown() =>
        Assert.Equal(6m, ScoreCalculator.Calculate(ExamKind.Ielts, 6.5m, 6.5m, 5.5m, 6m).Overall);

    [Fact]
    public void Ielts_RejectsWrongIncrement() =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => ScoreCalculator.Calculate(ExamKind.Ielts, 6.1m, 6m, 6m, 6m));

    [Fact]
    public void RejectsWrongNumberOfSections() =>
        Assert.Throws<ArgumentException>(
            () => ScoreCalculator.Calculate(ExamKind.Ielts, 6m, 6m));
}
