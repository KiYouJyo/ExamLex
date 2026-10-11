using System.Text.Json;
using ExamLex.Core;
using ExamLex.Services;
using ExamLex.Views;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace ExamLex;

public sealed partial class MainWindow : Window
{
    private readonly DictionaryStore _dictionary;
    private readonly string _glossaryPath;
    private readonly HashSet<string> _savedWords;
    private CancellationTokenSource? _searchCancellation;

    public MainWindow()
    {
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(TitleDragRegion);
        AppWindow.Resize(new Windows.Graphics.SizeInt32(1120, 746));

        _dictionary = new DictionaryStore();
        _glossaryPath = Path.Combine(Path.GetDirectoryName(_dictionary.DatabasePath)!, "glossary.json");
        _savedWords = ReadSavedWords();
    }

    private async void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _searchCancellation?.Cancel();
        _searchCancellation?.Dispose();
        _searchCancellation = new CancellationTokenSource();
        var cancellation = _searchCancellation.Token;

        try
        {
            string query = SearchBox.Text.Trim();
            if (query.Length == 0)
            {
                ShowEmpty("当前没有预装单词。词库已清空，等待后续导入。");
                return;
            }

            await Task.Delay(180, cancellation);
            var found = await _dictionary.SearchAsync(query, cancellation);
            cancellation.ThrowIfCancellationRequested();
            ResultsPanel.Children.Clear();
            foreach (var entry in found)
                ResultsPanel.Children.Add(CreateEntryCard(entry));

            if (found.Count == 0)
                ShowEmpty("没有找到这个单词。当前词库为空，可以在后续版本导入考试词库。");
            else
            {
                EmptyState.Visibility = Visibility.Collapsed;
                ResultsScroll.Visibility = Visibility.Visible;
            }
        }
        catch (OperationCanceledException) { /* Debounce: expected when input changes. */ }
        catch (Exception ex) { ShowEmpty("查询失败：" + ex.Message); }
    }

    private void ShowEmpty(string message)
    {
        EmptyDescription.Text = message;
        EmptyState.Visibility = Visibility.Visible;
        ResultsScroll.Visibility = Visibility.Collapsed;
        ResultsPanel.Children.Clear();
    }

    private UIElement CreateEntryCard(DictionaryEntry entry)
    {
        var body = new StackPanel { Spacing = 8 };
        var header = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 14 };
        header.Children.Add(new TextBlock
        {
            Text = entry.Word, FontSize = 30, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = Brush(240, 245, 242)
        });
        if (!string.IsNullOrWhiteSpace(entry.Phonetic))
            header.Children.Add(new TextBlock
            {
                Text = entry.Phonetic, FontSize = 18, VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brush(160, 178, 172)
            });
        body.Children.Add(header);

        AddOptional(body, entry.Translation, 20);
        AddOptional(body, entry.Definition, 17);
        if (!string.IsNullOrWhiteSpace(entry.Cefr))
            AddOptional(body, "CEFR · " + entry.Cefr, 15);
        if (entry.ExamTags.Count > 0)
            AddOptional(body, "Exam · " + string.Join(" · ", entry.ExamTags), 15);
        if (entry.Examples.Count > 0)
        {
            AddOptional(body, "例句", 15);
            foreach (string sentence in entry.Examples)
                AddOptional(body, sentence, 17);
        }

        var addButton = new Button { Content = "加入生词本", HorizontalAlignment = HorizontalAlignment.Right };
        addButton.Click += (_, _) =>
        {
            _savedWords.Add(entry.Word);
            SaveSavedWords();
            addButton.Content = "已收藏";
        };
        body.Children.Add(addButton);

        return new Border
        {
            Background = Brush(37, 47, 46),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(22, 18, 22, 14),
            Child = body
        };
    }

    private static SolidColorBrush Brush(byte r, byte g, byte b)
        => new(Color.FromArgb(255, r, g, b));

    private static void AddOptional(StackPanel body, string? value, double fontSize)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        body.Children.Add(new TextBlock
        {
            Text = value, TextWrapping = TextWrapping.Wrap, FontSize = fontSize,
            Foreground = Brush(197, 207, 203)
        });
    }

    private async void OnCalculatorClicked(object sender, RoutedEventArgs e)
    {
        var dialog = new ScoreCalculatorDialog { XamlRoot = Root.XamlRoot };
        await dialog.ShowAsync();
    }

    private async void OnGlossaryClicked(object sender, RoutedEventArgs e)
    {
        string words = _savedWords.Count == 0
            ? "生词本还是空的。后续导入词库后，可以在词条中收藏单词。"
            : string.Join("\n", _savedWords.OrderBy(w => w, StringComparer.OrdinalIgnoreCase));
        await new ContentDialog
        {
            XamlRoot = Root.XamlRoot, Title = "ExamLex · 生词本",
            Content = new ScrollViewer
            {
                MaxHeight = 400,
                Content = new TextBlock { Text = words, TextWrapping = TextWrapping.Wrap }
            },
            CloseButtonText = "关闭"
        }.ShowAsync();
    }

    private async void OnSettingsClicked(object sender, RoutedEventArgs e)
    {
        await new ContentDialog
        {
            XamlRoot = Root.XamlRoot, Title = "ExamLex · 设置",
            Content = new TextBlock
            {
                Text = "版本 0.1.0 · Windows WinUI 3\n" +
                       "词库初始为空，不包含 Flint / ECDICT 预装数据。\n" +
                       "本地目录：" + Path.GetDirectoryName(_dictionary.DatabasePath),
                TextWrapping = TextWrapping.Wrap
            },
            CloseButtonText = "关闭"
        }.ShowAsync();
    }

    private void OnPinClicked(object sender, RoutedEventArgs e)
    {
        if (AppWindow.Presenter is OverlappedPresenter presenter)
            presenter.IsAlwaysOnTop = PinToggle.IsChecked == true;
    }

    private HashSet<string> ReadSavedWords()
    {
        try
        {
            return new HashSet<string>(
                JsonSerializer.Deserialize<List<string>>(File.ReadAllText(_glossaryPath)) ?? [],
                StringComparer.OrdinalIgnoreCase);
        }
        catch (IOException) { return new(StringComparer.OrdinalIgnoreCase); }
        catch (JsonException) { return new(StringComparer.OrdinalIgnoreCase); }
    }

    private void SaveSavedWords()
    {
        File.WriteAllText(_glossaryPath,
            JsonSerializer.Serialize(_savedWords.OrderBy(w => w, StringComparer.OrdinalIgnoreCase)));
    }
}
