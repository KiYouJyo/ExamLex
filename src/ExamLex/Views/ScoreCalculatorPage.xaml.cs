using ExamLex.Core;
using ExamLex.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ExamLex.Views;

public sealed partial class ScoreCalculatorPage : Page
{
    private bool _busy;
    private Action? _backHome;
    public void Initialize(Action backHome) => _backHome=backHome;
    private void OnBackClicked(object sender,RoutedEventArgs e) => _backHome?.Invoke();
    public ScoreCalculatorPage()
    {
        InitializeComponent();
        Configure();
        MicaSurfaceService.Attach(this);
    }
    private ExamKind Exam => ExamSelector.SelectedIndex == 1 ? ExamKind.Ielts : ExamKind.Toefl2026;
    private void OnExamChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ReadingBox is not null) Configure();
    }
    private void Configure()
    {
        _busy = true;
        try
        {
            bool ielts = Exam == ExamKind.Ielts;
            foreach (var box in new[] { ReadingBox, ListeningBox, SpeakingBox, WritingBox })
            {
                box.Minimum = ielts ? 0 : 1;
                box.Maximum = ielts ? 9 : 6;
                box.SmallChange = 0.5;
                box.LargeChange = 0.5;
                box.Value = ielts ? 6 : 4;
            }
            RuleText.Text = ielts
                ? "IELTS 总分：四科平均，四舍五入到最近的 0.5 分；平均值以 .25 或 .75 结尾时向上进位。CEFR 是整体分数区间的大致对应，并非单独认证。"
                : "TOEFL iBT（2026-01-21 起）：四科采用 1–6、步长 0.5；总分为四科平均后取最近的 0.5 分。";
        }
        finally { _busy = false; }
        Recalculate();
    }
    private void OnScoreChanged(NumberBox sender, NumberBoxValueChangedEventArgs e)
    {
        if (!_busy && ResultText is not null) Recalculate();
    }
    private void Recalculate()
    {
        var values = new[] { ReadingBox,ListeningBox,SpeakingBox,WritingBox };
        if(values.Any(b=>double.IsNaN(b.Value)||double.IsInfinity(b.Value)))
        {
            ResultText.Text="请填写四科分数";CefrText.Text="";ComparisonText.Text="";return;
        }
        try
        {
            var result = ScoreCalculator.Calculate(Exam,values.Select(b=>(decimal)b.Value).ToArray());
            ResultText.Text = $"{result.Overall:0.#} / {(Exam==ExamKind.Ielts?"9":"6")}";
            CefrText.Text = result.Cefr is null ? "" : $"CEFR · {result.Cefr}";
            ComparisonText.Text = result.Comparison ?? "";
        }
        catch(ArgumentException)
        {
            ResultText.Text="输入不符合分数范围";CefrText.Text="";ComparisonText.Text="";
        }
        catch(OverflowException)
        {
            ResultText.Text="无效分数";CefrText.Text="";ComparisonText.Text="";
        }
    }
}
