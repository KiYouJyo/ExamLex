using ExamLex.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.System;
using System.Diagnostics;
using Windows.Media.SpeechSynthesis;

namespace ExamLex.Views;

public sealed partial class SettingsPage : Page
{
    private AppSettings? _settings;
    private LexiconUpdateService? _lexicon;
    private Action? _applySettings;
    private Action? _dictionaryChanged;
    private bool _ready;
    private Action? _backHome;
    public SettingsPage()
    {
        InitializeComponent();
        Loaded += (_, _) => DispatcherQueue.TryEnqueue(() => SettingsScroll.ChangeView(null, 0, null, true));
    }

    public void Initialize(AppSettings settings, DictionaryStore store, Action applySettings,
        Action dictionaryChanged, Action backHome)
    {
        _ready = false;
        var version = typeof(SettingsPage).Assembly.GetName().Version;
        AppVersionText.Text = version is null ? "ExamLex" : $"ExamLex {version.Major}.{version.Minor}.{version.Build}";
        _settings=settings;
        _lexicon=new LexiconUpdateService(store);
        _applySettings=applySettings;
        _dictionaryChanged=dictionaryChanged;
        _backHome=backHome;
        ThemeBox.SelectedIndex=Math.Clamp(settings.Appearance,0,2);
        BackdropBox.SelectedIndex=settings.Backdrop==2?1:0;
        // Flint's three illustrated RadioButton previews, not a new ComboBox.
        switch (Math.Clamp(settings.SearchStyle,0,2))
        {
            case 0: ModernStyleRadio.IsChecked=true; break;
            case 1: RoundStyleRadio.IsChecked=true; break;
            default: ClassicStyleRadio.IsChecked=true; break;
        }
        DefinitionSwitch.IsOn=settings.ShowEnglishDefinition;
        EnableGlossaryToggleSwitch.IsOn=settings.EnableGlossary;
        EnableTTSToggleSwitch.IsOn=settings.EnableTts;
        VolumeSlider.Value=Math.Clamp(settings.TtsVolume/10d,1,10);
        ClearSwitch.IsOn=settings.AutoClearSearch;
        TTSVoicesComboBox.Items.Clear();
        TTSVoicesComboBox.Items.Add("默认声音");
        TTSVoicesComboBox.SelectedIndex=0;
        DatabaseVersionText.Text="本机词库："+_lexicon.InstalledVersion;
        _ready=true;
    }
    private void SavePreferences()
    {
        if(!_ready || _settings is null) return;
        _settings.Appearance=ThemeBox.SelectedIndex;
        _settings.Backdrop=BackdropBox.SelectedIndex==1?2:1;
        _settings.SearchStyle=ModernStyleRadio.IsChecked==true?0:RoundStyleRadio.IsChecked==true?1:2;
        _settings.ShowEnglishDefinition=DefinitionSwitch.IsOn;
        _settings.EnableGlossary=EnableGlossaryToggleSwitch.IsOn;
        _settings.EnableTts=EnableTTSToggleSwitch.IsOn;
        _settings.AutoClearSearch=ClearSwitch.IsOn;
        _settings.TtsVolume=(int)Math.Round(VolumeSlider.Value*10);
        _settings.Save();
        _applySettings?.Invoke();
    }
    private void OnPreferenceChanged(object sender, RoutedEventArgs e) => SavePreferences();
    private void OnPreferenceChanged(object sender, SelectionChangedEventArgs e) => SavePreferences();
    private void OnVolumeChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        => SavePreferences();

    private void OnSearchStyleChecked(object sender, RoutedEventArgs e) => SavePreferences();

    private void OnSettingsScrollChanged(object sender, ScrollViewerViewChangedEventArgs e)
    {
        // Flint3 SettingsPage.xaml.cs's scroll-driven sticky title behavior.
        if (sender is ScrollViewer viewer)
        {
            double maxOffset = Math.Min(160, viewer.ScrollableHeight);
            if (maxOffset <= 0) return;
            double opacity = Math.Clamp(viewer.VerticalOffset/maxOffset,0,1);
            SettingsPageHeaderTitleGrid.Opacity=opacity;
            SettingsPageHeaderSeperatorLineBorder.Opacity=opacity;
        }
    }

    private void OnVoiceChanged(object sender, SelectionChangedEventArgs e) { }

    private async void OnHearVoiceClicked(object sender, RoutedEventArgs e)
    {
        try
        {
            using var synth = new SpeechSynthesizer();
            var stream = await synth.SynthesizeTextToStreamAsync("ExamLex");
            using var player = new Windows.Media.Playback.MediaPlayer();
            player.Source = Windows.Media.Core.MediaSource.CreateFromStream(stream,stream.ContentType);
            player.Volume=(_settings?.TtsVolume ?? 80)/100d;
            player.Play();
            await Task.Delay(1200);
        }
        catch (Exception ex) { Debug.WriteLine("朗读失败："+ex.Message); }
    }

    private void OnBackClicked(object sender, RoutedEventArgs e) => _backHome?.Invoke();

    private void OnGlossaryFolderClicked(object sender, RoutedEventArgs e)
    {
        string directory = Path.GetDirectoryName(AppSettings.FilePath)!;
        Directory.CreateDirectory(directory);
        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"\"{directory}\"",
            UseShellExecute = true
        });
    }

    private async void OnSourceClicked(object sender,RoutedEventArgs e) =>
        await Launcher.LaunchUriAsync(new Uri("https://github.com/KiYouJyo/ExamLex"));

    private async void OnAppUpdateClicked(object sender, RoutedEventArgs e)
    {
        try
        {
            var result=await AppUpdateService.CheckAsync();
            if(result.Newer)
            {
                await Launcher.LaunchUriAsync(new Uri(result.Url));
                return;
            }
            var dialog=new ContentDialog
            {
                XamlRoot=XamlRoot, Title="ExamLex · 检查更新",
                Content=new TextBlock{Text=$"当前 {AppVersionText.Text}。{result.Version}。",TextWrapping=TextWrapping.Wrap},
                CloseButtonText="确定"
            };
            await dialog.ShowAsync();
        }
        catch(Exception ex)
        {
            await new ContentDialog
            {
                XamlRoot=XamlRoot,Title="检查更新失败",
                Content=new TextBlock{Text=ex.Message,TextWrapping=TextWrapping.Wrap},
                CloseButtonText="确定"
            }.ShowAsync();
        }
    }

    private async Task ShowLexiconMessageAsync(string message)
    {
        await new ContentDialog
        {
            XamlRoot=XamlRoot,Title="ExamLex-Lexicon",
            Content=new TextBlock{Text=message,TextWrapping=TextWrapping.Wrap},
            CloseButtonText="确定"
        }.ShowAsync();
    }

    private async void OnDatabaseUpdateClicked(object sender,RoutedEventArgs e)
    {
        if(_lexicon is null)return;
        DatabaseUpdateButton.IsEnabled=false;
        DatabaseUpdateStatus.Text="正在检查 ExamLex-Lexicon 的公开词库版本…";
        try
        {
            var available=await _lexicon.CheckAsync();
            if(!available.UpdateAvailable)
            {
                await ShowLexiconMessageAsync($"已是最新词库 {available.Version}（{available.WordCount} 词）。");
                return;
            }
            var dialog = new ContentDialog
            {
                XamlRoot=XamlRoot,
                Title="下载词库更新",
                Content=new TextBlock{
                    Text=$"版本：{available.Version}\n包含 {available.WordCount} 条词条。\n{available.Description}\n\n将校验 SHA256 和 SQLite Schema，通过后才替换本地词库。",
                    TextWrapping=TextWrapping.Wrap
                },
                PrimaryButtonText="下载并安装",
                CloseButtonText="取消",
                DefaultButton=ContentDialogButton.Primary
            };
            if(await dialog.ShowAsync()!=ContentDialogResult.Primary)
            {
                DatabaseUpdateStatus.Text="已取消词库更新。";
                return;
            }
            DatabaseUpdateStatus.Text="正在下载并校验数据库，请稍候…";
            await _lexicon.InstallAsync(available);
            DatabaseVersionText.Text="本机词库："+_lexicon.InstalledVersion;
            await ShowLexiconMessageAsync($"数据库更新完成 · {available.WordCount} 词。返回主页即可查词。");
            _dictionaryChanged?.Invoke();
        }
        catch(Exception ex){await ShowLexiconMessageAsync("数据库检查/更新失败："+ex.Message);}
        finally{DatabaseUpdateButton.IsEnabled=true;}
    }
}
