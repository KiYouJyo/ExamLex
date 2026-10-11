using ExamLex.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ExamLex.Views;

public sealed partial class ScoreCalculatorDialog : ContentDialog
{
    private bool _updating;

    public ScoreCalculatorDialog()
    {
        InitializeComponent();
        ExamSelector.SelectedIndex = 0;
        Configure();
    }

    private ExamKind Exam => ExamSelector.SelectedIndex switch
    {
        1 => ExamKind.Ielts,
        2 => ExamKind.ToeicListeningReading,
        _ => ExamKind.Toefl2026
    };

    private void OnExamChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ReadingBox is not null)
            Configure();
    }

    private void Configure()
    {
        _updating = true;
        try
        {
            bool toeic = Exam == ExamKind.ToeicListeningReading;
            SpeakingRow.Visibility = toeic ? Visibility.Collapsed : Visibility.Visible;
            WritingRow.Visibility = toeic ? Visibility.Collapsed : Visibility.Visible;

            double min = toeic ? 5 : Exam == ExamKind.Ielts ? 0 : 1;
            double max = toeic ? 495 : Exam == ExamKind.Ielts ? 9 : 6;
            double step = toeic ? 5 : 0.5;
            double initial = toeic ? 350 : Exam == ExamKind.Ielts ? 6 : 4;

            foreach (var box in new[] { ReadingBox, ListeningBox, SpeakingBox, WritingBox })
            {
                box.Minimum = min;
                box.Maximum = max;
                box.SmallChange = step;
                box.LargeChange = step;
                box.Value = initial;
            }

            RuleText.Text = Exam switch
            {
                ExamKind.Toefl2026 => "托福新制（2026-01-21 起）：四科均为 1–6、步长 0.5；总分按四科平均四舍五入至最近的 0.5。",
                ExamKind.Ielts => "雅思：四科 0–9、步长 0.5；四科均值以 0.5 为单位取最近值，0.25 和 0.75 向上进位。",
                _ => "托业 L&R：听力和阅读各 5–495、步长 5，直接相加为总分（10–990）。"
            };
        }
        finally { _updating = false; }

        UpdateResult();
    }

    private void OnScoreChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (!_updating && ResultText is not null)
            UpdateResult();
    }

    private void UpdateResult()
    {
        if (ResultText is null) return;

        var boxes = Exam == ExamKind.ToeicListeningReading
            ? new[] { ReadingBox, ListeningBox }
            : new[] { ReadingBox, ListeningBox, SpeakingBox, WritingBox };

        if (boxes.Any(b => double.IsNaN(b.Value) || double.IsInfinity(b.Value)))
        {
            ResultText.Text = "请输入有效分数";
            CefrText.Text = "";
            ComparisonText.Text = "";
            return;
        }

        try
        {
            var values = boxes.Select(b => (decimal)b.Value).ToArray();
            ScoreResult result = ScoreCalculator.Calculate(Exam, values);
            ResultText.Text = $"{result.Overall:0.#}  /  {result.Scale.Split('–').Last()}";
            CefrText.Text = result.Cefr is null ? "" : "CEFR 等级：" + result.Cefr;
            ComparisonText.Text = result.Comparison ?? "";
        }
        catch (ArgumentException)
        {
            ResultText.Text = "分数超出范围或不符合步长";
            CefrText.Text = "";
            ComparisonText.Text = "";
        }
        catch (OverflowException)
        {
            ResultText.Text = "请输入有效分数";
            CefrText.Text = "";
            ComparisonText.Text = "";
        }
    }
}
