using System.Text.Json;
using System.Runtime.InteropServices;
using ExamLex.Core;
using ExamLex.Services;
using ExamLex.Views;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media.SpeechSynthesis;
using Windows.UI;

namespace ExamLex;

public sealed partial class MainWindow : Window
{
    private readonly DictionaryStore _dictionary;
    private readonly AppSettings _preferences;
    private readonly string _glossaryPath;
    private readonly HashSet<string> _savedWords;
    private CancellationTokenSource? _searchCancellation;
    private bool _sized;
    private MediaPlayer? _speaker;

    public MainWindow()
    {
        InitializeComponent();
        ExtendsContentIntoTitleBar=true;
        SetTitleBar(TitleDragRegion);
        _dictionary=new DictionaryStore();
        _preferences=AppSettings.Load();
        _glossaryPath=Path.Combine(_dictionary.DataDirectory,"glossary.json");
        _savedWords=ReadSavedWords();
        ApplyPreferences();
        Activated+=OnWindowActivated;
    }

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);

    private void OnWindowActivated(object sender, WindowActivatedEventArgs e)
    {
        if (_sized) return;
        _sized = true;

        // AppWindow takes *physical* pixels. XamlRoot.RasterizationScale can
        // still be 1 during the first Activated event on a high-DPI display.
        // Read the actual HWND DPI instead, then size in effective pixels.
        IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        uint dpi = GetDpiForWindow(hwnd);
        double scale = dpi >= 96 ? dpi / 96d : 1d;

        var display = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest);
        var work = display.WorkArea;
        int width = Math.Min((int)Math.Round(660 * scale), Math.Max(440, work.Width - 32));
        int height = Math.Min((int)Math.Round(470 * scale), Math.Max(340, work.Height - 32));
        int x = work.X + Math.Max(0, (work.Width - width) / 2);
        int y = work.Y + Math.Max(0, (work.Height - height) / 2);
        AppWindow.MoveAndResize(new Windows.Graphics.RectInt32(x, y, width, height));
    }

    private void ApplyPreferences()
    {
        Root.RequestedTheme=_preferences.Appearance switch{
            1=>ElementTheme.Dark,2=>ElementTheme.Light,_=>ElementTheme.Default
        };
        bool light = _preferences.Appearance==2 ||
            (_preferences.Appearance==0 && (Application.Current.RequestedTheme==ApplicationTheme.Light));
        Root.Background=new SolidColorBrush(light?
            Color.FromArgb(255,247,249,249):Color.FromArgb(255,30,30,30));
        SearchHost.Background=new SolidColorBrush(light?
            Color.FromArgb(255,238,244,243):Color.FromArgb(255,37,37,37));
        SearchHost.BorderBrush=new SolidColorBrush(light?
            Color.FromArgb(255,211,222,219):Color.FromArgb(255,59,59,59));
        Footer.Background=new SolidColorBrush(light?
            Color.FromArgb(255,239,245,244):Color.FromArgb(255,31,31,31));
        SearchHost.CornerRadius=new CornerRadius(_preferences.SearchStyle switch{1=>18,2=>2,_=>6});
        SystemBackdrop=_preferences.Backdrop switch
        {
            1=>new MicaBackdrop(),
            2=>new DesktopAcrylicBackdrop(),
            _=>null
        };
        if(_preferences.Backdrop!=0)
            Root.Background=new SolidColorBrush(Colors.Transparent);
        if(_speaker is not null) _speaker.Volume=_preferences.TtsVolume/100d;
        if(ContentFrame.Content is SettingsPage settingsPage)
        {
            // Settings page owns preference controls; no additional work required.
        }
    }

    private void NavigateTo(Type type,string title)
    {
        SearchHost.Visibility=Visibility.Collapsed;
        HomePanel.Visibility=Visibility.Collapsed;
        Footer.Visibility=Visibility.Collapsed;
        ContentFrame.Visibility=Visibility.Visible;
        BackButton.Visibility=Visibility.Visible;
        HeaderTextBlock.Text=type==typeof(SettingsPage)?"Settings":title;
        HeaderTextBlock.FontFamily=new Microsoft.UI.Xaml.Media.FontFamily(
            type==typeof(SettingsPage)?"Times New Roman":"Segoe UI");
        HeaderTextBlock.FontSize=20;
        HeaderTextBlock.Margin=new Thickness(20,0,0,4);
        HeaderTextBlock.VerticalAlignment=VerticalAlignment.Bottom;
        BackButton.Visibility=type==typeof(SettingsPage)?Visibility.Collapsed:Visibility.Visible;
        ContentFrame.Navigate(type);
        if(ContentFrame.Content is SettingsPage settings)
            settings.Initialize(_preferences,_dictionary,ApplyPreferences,OnDictionaryChanged,ShowHome);
    }
    private void OnDictionaryChanged()
    {
        // On returning home, search again using the newly installed file.
    }
    private void ShowHome()
    {
        ContentFrame.Visibility=Visibility.Collapsed;
        ContentFrame.Content=null;
        Footer.Visibility=Visibility.Visible;
        HomePanel.Visibility=Visibility.Visible;
        SearchHost.Visibility=Visibility.Visible;
        BackButton.Visibility=Visibility.Collapsed;
        HeaderTextBlock.Text="ExamLex";
        HeaderTextBlock.FontFamily=new Microsoft.UI.Xaml.Media.FontFamily("Times New Roman");
        HeaderTextBlock.FontSize=20;
        HeaderTextBlock.Margin=new Thickness(20,0,0,4);
        if(_preferences.AutoClearSearch) SearchBox.Text="";
        _ = RefreshSearchAsync(SearchBox.Text);
    }
    private void OnBackClicked(object sender,RoutedEventArgs e)=>ShowHome();

    private void OnCalculatorClicked(object sender,RoutedEventArgs e)
    {
        if(ContentFrame.Content is ScoreCalculatorPage){ShowHome();return;}
        NavigateTo(typeof(ScoreCalculatorPage),"算分器");
    }
    private void OnSettingsClicked(object sender,RoutedEventArgs e)
    {
        if(ContentFrame.Content is SettingsPage){ShowHome();return;}
        NavigateTo(typeof(SettingsPage),"设置");
    }
    private void OnSearchTextChanged(object sender,TextChangedEventArgs e) =>
        _ = RefreshSearchAsync(SearchBox.Text);
    private async Task RefreshSearchAsync(string search)
    {
        _searchCancellation?.Cancel();
        _searchCancellation?.Dispose();
        _searchCancellation=new CancellationTokenSource();
        var ct=_searchCancellation.Token;
        try
        {
            if(string.IsNullOrWhiteSpace(search))
            {
                ShowEmpty(File.Exists(_dictionary.DatabasePath)?
                    "输入英文单词以查询 ExamLex-Lexicon。":
                    "还没有安装词库。可在设置 → 关于中检查 ExamLex-Lexicon 更新。");
                return;
            }
            await Task.Delay(180,ct);
            var results=await _dictionary.SearchAsync(search,ct);
            ct.ThrowIfCancellationRequested();
            ResultsPanel.Children.Clear();
            foreach(var entry in results)
                ResultsPanel.Children.Add(CreateEntryCard(entry));
            if(results.Count==0)ShowEmpty(File.Exists(_dictionary.DatabasePath)?
                "当前词库中没有找到该单词。":"请先在设置中安装 ExamLex-Lexicon 词库。");
            else
            {
                EmptyState.Visibility=Visibility.Collapsed;
                ResultsScroll.Visibility=Visibility.Visible;
            }
        }
        catch(OperationCanceledException) { }
        catch(Exception ex){ShowEmpty("查询失败："+ex.Message);}
    }
    private void ShowEmpty(string message)
    {
        EmptyDescription.Text=message;
        EmptyState.Visibility=Visibility.Visible;
        ResultsScroll.Visibility=Visibility.Collapsed;
        ResultsPanel.Children.Clear();
    }
    // The established ExamLex word-entry contract is always rendered, even
    // when the current source database has not filled a section yet.
    // Never flatten senses or put unrelated examples at the end of a word.
    private UIElement CreateEntryCard(DictionaryEntry entry)
    {
        var content = new StackPanel { Spacing = 8 };

        var header = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
        header.Children.Add(new TextBlock {
            Text = entry.Word, FontSize = 28,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        });
        if (!string.IsNullOrWhiteSpace(entry.Cefr))
            header.Children.Add(new TextBlock {
                Text = entry.Cefr, FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center, Opacity = 0.72
            });
        content.Children.Add(header);

        var pronunciations = entry.Pronunciations
            .Select(p => (p.Accent is null ? "" : p.Accent + "  ") + p.Ipa)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        AddField(content, "音标 · Pronunciation",
            pronunciations.Count == 0 ? null : string.Join("  ·  ", pronunciations.Take(4))
            + (pronunciations.Count > 4 ? $"  · 另有 {pronunciations.Count - 4} 条发音记录" : ""));

        // 1. Senses grouped by the actual source part of speech.
        // Every sense always reserves its own Chinese, Japanese and English fields.
        if (entry.Senses.Count == 0)
        {
            AddSectionTitle(content, "词性与义项 · Parts of speech & meanings");
            AddMissing(content, "暂无义项，等待词库补充。");
        }
        else
        {
            foreach (var group in entry.Senses.GroupBy(x => x.PartOfSpeech))
            {
                AddSectionTitle(content, PosTitle(group.Key));
                foreach (var sense in group)
                {
                    var sensePanel = new StackPanel { Spacing = 6 };
                    AddCaption(sensePanel, "义项 " + sense.Number);
                    AddField(sensePanel, "中文", JoinTranslations(sense.Translations, "zh"));
                    AddField(sensePanel, "日本語", JoinTranslations(sense.Translations, "ja"));
                    AddField(sensePanel, "English",
                        _preferences.ShowEnglishDefinition ? sense.EnglishDefinition
                            : "已在设置中隐藏英文释义");
                    content.Children.Add(Subcard(sensePanel));
                }
            }
        }

        // 2. Morphology — permanently reserved, not hidden on empty datasets.
        AddSectionTitle(content, "词形变化 · Inflections");
        if (entry.Inflections.Count == 0) AddMissing(content, "待补充");
        else
            foreach (var item in entry.Inflections)
                AddField(content, PosShort(item.PartOfSpeech) + " · " +
                    FormatFormType(item.FormType), item.Form);

        // 3. Semantic relationships; each record names its parent sense.
        AddSectionTitle(content, "近义词 · Synonyms");
        AddRelations(content, entry.Senses, "synonym");
        AddSectionTitle(content, "反义词 · Antonyms");
        AddRelations(content, entry.Senses, "antonym");

        // 4. Valency patterns and phrases remain separate, as agreed.
        AddSectionTitle(content, "配价 · Valency patterns");
        AddExpressions(content, entry.Senses, "valency");
        AddSectionTitle(content, "词组与搭配 · Phrases & collocations");
        AddExpressions(content, entry.Senses, "phrases");

        // 5. Examples keep their sense, Chinese and Japanese alignment.
        AddSectionTitle(content, "例句 · Examples");
        bool hasExamples = false;
        foreach (var sense in entry.Senses)
        {
            foreach (var example in sense.Examples)
            {
                hasExamples = true;
                var panel = new StackPanel { Spacing = 5 };
                AddCaption(panel, SenseLabel(sense));
                AddField(panel, "English", example.English);
                AddField(panel, "中文", JoinTranslations(example.Translations, "zh"));
                AddField(panel, "日本語", JoinTranslations(example.Translations, "ja"));
                content.Children.Add(Subcard(panel));
            }
        }
        if (!hasExamples) AddMissing(content, "待补充");

        AddSectionTitle(content, "考试标签 · Exam tags");
        AddMissing(content, entry.ExamTags.Count > 0
            ? string.Join(" · ", entry.ExamTags) : "待补充");

        var controls = new StackPanel {
            Orientation = Orientation.Horizontal, Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        if (_preferences.EnableTts)
        {
            var speak = new Button { Content = "朗读" };
            speak.Click += async (_, _) => await SpeakAsync(entry.Word);
            controls.Children.Add(speak);
        }
        if (_preferences.EnableGlossary)
        {
            var save = new Button { Content = _savedWords.Contains(entry.Word) ? "已收藏" : "加入生词本" };
            save.Click += (_, _) => {
                _savedWords.Add(entry.Word);
                SaveSavedWords();
                save.Content = "已收藏";
            };
            controls.Children.Add(save);
        }
        content.Children.Add(controls);
        return new Border {
            Background = Brush(240, 246, 244, 37, 47, 46),
            BorderBrush = Brush(214, 224, 221, 59, 66, 64),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(16),
            Child = content
        };
    }

    private static string PosShort(string pos) => pos switch {
        "noun" => "n.", "verb" => "v.", "adj" => "adj.",
        "adv" => "adv.", "prep" => "prep.", "pron" => "pron.",
        "det" => "det.", "conj" => "conj.", "intj" => "interj.",
        "num" => "num.", "article" => "art.", _ => pos
    };

    private static string PosTitle(string pos) => pos switch {
        "noun" => "Noun · 名词 · 名詞",
        "verb" => "Verb · 动词 · 動詞",
        "adj" => "Adjective · 形容词 · 形容詞",
        "adv" => "Adverb · 副词 · 副詞",
        "prep" => "Preposition · 介词 · 前置詞",
        "pron" => "Pronoun · 代词 · 代名詞",
        "det" => "Determiner · 限定词 · 限定詞",
        "conj" => "Conjunction · 连词 · 接続詞",
        "intj" => "Interjection · 感叹词 · 間投詞",
        "num" => "Numeral · 数词 · 数詞",
        "article" => "Article · 冠词 · 冠詞",
        _ => pos + " · 其他词性"
    };

    private static string SenseLabel(DictionarySense sense) =>
        PosShort(sense.PartOfSpeech) + " · 义项 " + sense.Number;

    private static string? JoinTranslations(
        IReadOnlyList<DictionaryTranslation> items, string language)
    {
        var texts = items.Where(t => t.Language == language)
            .Select(t => t.Text).Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct().ToList();
        return texts.Count == 0 ? null : string.Join("；", texts);
    }

    private static string FormatFormType(string type) => type.Replace(";", " · ")
        .Replace('-', ' ');

    private void AddRelations(
        StackPanel panel, IReadOnlyList<DictionarySense> senses, string relationType)
    {
        bool found = false;
        foreach (var sense in senses)
        {
            var words = sense.Relations.Where(r => r.Type == relationType)
                .Select(r => r.RelatedWord).Distinct().ToList();
            if (words.Count == 0) continue;
            found = true;
            AddField(panel, SenseLabel(sense), string.Join(" · ", words));
        }
        if (!found) AddMissing(panel, "待补充");
    }

    private void AddExpressions(
        StackPanel panel, IReadOnlyList<DictionarySense> senses, string selection)
    {
        bool found = false;
        foreach (var sense in senses)
        {
            var expressions = sense.Expressions.Where(x => selection == "valency"
                ? x.Type == "valency"
                : x.Type != "valency").ToList();
            foreach (var expression in expressions)
            {
                found = true;
                var item = new StackPanel { Spacing = 4 };
                AddCaption(item, SenseLabel(sense) + " · " + expression.Type);
                AddField(item, "English", expression.English);
                AddField(item, "中文", expression.Chinese);
                AddField(item, "日本語", expression.Japanese);
                if (!string.IsNullOrWhiteSpace(expression.Notes))
                    AddField(item, "Notes", expression.Notes);
                panel.Children.Add(Subcard(item));
            }
        }
        if (!found) AddMissing(panel, "待补充");
    }

    private void AddSectionTitle(StackPanel panel, string title)
    {
        panel.Children.Add(new Border {
            Margin = new Thickness(0, 12, 0, 2),
            Padding = new Thickness(0, 10, 0, 1),
            BorderThickness = new Thickness(0, 1, 0, 0),
            BorderBrush = Brush(213, 220, 218, 63, 70, 67),
            Child = new TextBlock {
                Text = title, FontSize = 17,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap
            }
        });
    }

    private static Border Subcard(UIElement child) => new() {
        Margin = new Thickness(0, 3, 0, 3),
        Padding = new Thickness(10, 9, 10, 10),
        CornerRadius = new CornerRadius(6),
        Background = new SolidColorBrush(Color.FromArgb(26, 144, 171, 164)),
        Child = child
    };

    private static void AddCaption(StackPanel panel, string text) =>
        panel.Children.Add(new TextBlock {
            Text = text, FontSize = 12, Opacity = 0.60,
            TextWrapping = TextWrapping.Wrap
        });

    private static void AddMissing(StackPanel panel, string text) =>
        panel.Children.Add(new TextBlock {
            Text = text, FontSize = 13, Opacity = 0.54,
            TextWrapping = TextWrapping.Wrap
        });

    private static void AddField(StackPanel panel, string label, string? value)
    {
        var row = new StackPanel { Spacing = 3 };
        row.Children.Add(new TextBlock {
            Text = label + "：", FontSize = 12, Opacity = 0.65
        });
        row.Children.Add(new TextBlock {
            Text = string.IsNullOrWhiteSpace(value) ? "待补充" : value,
            FontSize = 15, TextWrapping = TextWrapping.Wrap,
            Opacity = string.IsNullOrWhiteSpace(value) ? 0.45 : 0.95
        });
        panel.Children.Add(row);
    }

    private SolidColorBrush Brush(byte lr, byte lg, byte lb, byte dr, byte dg, byte db) =>
        new(Root.RequestedTheme == ElementTheme.Light
            ? Color.FromArgb(255, lr, lg, lb)
            : Color.FromArgb(255, dr, dg, db));

    private async Task SpeakAsync(string word)
    {
        try
        {
            using var synth=new SpeechSynthesizer();
            var speech=await synth.SynthesizeTextToStreamAsync(word);
            _speaker ??= new MediaPlayer();
            _speaker.Volume=_preferences.TtsVolume/100d;
            _speaker.Source=MediaSource.CreateFromStream(speech,speech.ContentType);
            _speaker.Play();
        }
        catch(Exception ex){ShowEmpty("朗读失败："+ex.Message);}
    }
    private async void OnGlossaryClicked(object sender,RoutedEventArgs e)
    {
        var words=_savedWords.Count==0?
            "生词本还是空的。先安装词库并添加收藏。":
            string.Join("\n",_savedWords.OrderBy(x=>x,StringComparer.OrdinalIgnoreCase));
        await new ContentDialog{
            XamlRoot=Root.XamlRoot,Title="ExamLex · 生词本",
            Content=new ScrollViewer{MaxHeight=360,Content=new TextBlock{Text=words,TextWrapping=TextWrapping.Wrap}},
            CloseButtonText="返回"
        }.ShowAsync();
    }
    private void OnPinClicked(object sender,RoutedEventArgs e)
    {
        if(AppWindow.Presenter is OverlappedPresenter presenter)
            presenter.IsAlwaysOnTop=PinToggle.IsChecked==true;
    }
    private HashSet<string> ReadSavedWords()
    {
        try{return new HashSet<string>(JsonSerializer.Deserialize<List<string>>(
            File.ReadAllText(_glossaryPath))??[],StringComparer.OrdinalIgnoreCase);}
        catch(Exception ex) when(ex is IOException or JsonException)
        {return new HashSet<string>(StringComparer.OrdinalIgnoreCase);}
    }
    private void SaveSavedWords() =>
        File.WriteAllText(_glossaryPath,JsonSerializer.Serialize(_savedWords.OrderBy(
            x=>x,StringComparer.OrdinalIgnoreCase)));
}
