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
    private UIElement CreateEntryCard(DictionaryEntry entry)
    {
        var content=new StackPanel{Spacing=7};
        var title=new StackPanel{Orientation=Orientation.Horizontal,Spacing=12};
        title.Children.Add(new TextBlock{
            Text=entry.Word,FontSize=26,
            FontWeight=Microsoft.UI.Text.FontWeights.SemiBold
        });
        if(!string.IsNullOrWhiteSpace(entry.Phonetic))
            title.Children.Add(new TextBlock{
                Text=entry.Phonetic,FontSize=16,Opacity=0.72,
                VerticalAlignment=VerticalAlignment.Center
            });
        content.Children.Add(title);
        AddText(content,entry.Translation,18);
        if(_preferences.ShowEnglishDefinition)AddText(content,entry.Definition,15);
        if(!string.IsNullOrWhiteSpace(entry.Cefr))AddText(content,"CEFR · "+entry.Cefr,14);
        if(entry.ExamTags.Count>0)AddText(content,"Exam · "+string.Join(" · ",entry.ExamTags),14);
        if(entry.Examples.Count>0)
        {
            AddText(content,"例句",13);
            foreach(var sentence in entry.Examples)AddText(content,sentence,15);
        }
        var controls=new StackPanel{Orientation=Orientation.Horizontal,Spacing=8,
            HorizontalAlignment=HorizontalAlignment.Right};
        if(_preferences.EnableTts)
        {
            var speak=new Button{Content="朗读"};
            speak.Click+=async (_,_)=>await SpeakAsync(entry.Word);
            controls.Children.Add(speak);
        }
        if(_preferences.EnableGlossary)
        {
            var save=new Button{Content=_savedWords.Contains(entry.Word)?"已收藏":"加入生词本"};
            save.Click+=(_,_)=>{
                _savedWords.Add(entry.Word);SaveSavedWords();save.Content="已收藏";
            };
            controls.Children.Add(save);
        }
        content.Children.Add(controls);
        return new Border{
            Background=new SolidColorBrush(Root.RequestedTheme==ElementTheme.Light?
                Color.FromArgb(255,234,242,240):Color.FromArgb(255,37,47,46)),
            CornerRadius=new CornerRadius(10),Padding=new Thickness(20),
            Child=content
        };
    }
    private static void AddText(StackPanel panel,string? text,double size)
    {
        if(string.IsNullOrWhiteSpace(text))return;
        panel.Children.Add(new TextBlock{Text=text,FontSize=size,Opacity=0.85,TextWrapping=TextWrapping.Wrap});
    }
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
