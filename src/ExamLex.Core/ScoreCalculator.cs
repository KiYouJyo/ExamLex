namespace ExamLex.Core;

public enum ExamKind { Toefl2026, Ielts }

public sealed record ScoreResult(
    ExamKind Exam,
    decimal Overall,
    string Scale,
    string? Cefr = null,
    string? Comparison = null);

/// <summary>Score aggregation only; never predicts raw-test performance or official score reports.</summary>
public static class ScoreCalculator
{
    // ETS overall-band lower bounds of the historical 0–120 concordance scale.
    private static readonly int[] ToeflOldLowerBounds =
        [0, 12, 24, 34, 44, 58, 72, 86, 95, 107, 114];

    public static ScoreResult Calculate(ExamKind exam, params decimal[] sections)
        => Calculate(exam, (IReadOnlyList<decimal>)sections);

    public static ScoreResult Calculate(ExamKind exam, IReadOnlyList<decimal> sections)
    {
        ArgumentNullException.ThrowIfNull(sections);
        int expected = 4;
        if (sections.Count != expected)
            throw new ArgumentException($"Expected {expected} section scores.", nameof(sections));

        switch (exam)
        {
            case ExamKind.Toefl2026:
                CheckSections(sections, 1m, 6m, 0.5m);
                decimal toefl = RoundHalf(sections.Average());
                int index = checked((int)((toefl - 1m) * 2m));
                int lower = ToeflOldLowerBounds[index];
                int upper = index == ToeflOldLowerBounds.Length - 1
                    ? 120 : ToeflOldLowerBounds[index + 1] - 1;
                string cefr = toefl switch
                {
                    < 2m => "A1", < 3m => "A2", < 4m => "B1",
                    < 5m => "B2", < 6m => "C1", _ => "C2"
                };
                return new(ExamKind.Toefl2026, toefl, "1–6", cefr,
                    $"旧制参考区间：{lower}–{upper} / 120（非正式换算）");

            case ExamKind.Ielts:
                CheckSections(sections, 0m, 9m, 0.5m);
                return new(ExamKind.Ielts, RoundHalf(sections.Average()), "0–9",
                    Comparison: "四科平均分按 0.5 分档四舍五入；.25 / .75 向上进位。");


            default:
                throw new ArgumentOutOfRangeException(nameof(exam));
        }
    }

    private static decimal RoundHalf(decimal value)
        => decimal.Round(value * 2m, 0, MidpointRounding.AwayFromZero) / 2m;

    private static void CheckSections(IEnumerable<decimal> scores, decimal min, decimal max, decimal step)
    {
        foreach (decimal score in scores)
        {
            if (score < min || score > max || score % step != 0m)
                throw new ArgumentOutOfRangeException(nameof(scores),
                    $"Scores must be {min}–{max} in {step} increments.");
        }
    }
}
