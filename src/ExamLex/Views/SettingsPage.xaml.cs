using ExamLex.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.System;
using System.Diagnostics;

namespace ExamLex.Views;

public sealed partial class SettingsPage : Page
{
    private AppSettings? _settings;
    private LexiconUpdateService? _lexicon;
    private Action? _applySettings;
    private Action? _dictionaryChanged;
    private bool _ready;
    private Action? _backHome;
    public SettingsPage() => InitializeComponent();

    public void Initialize(AppSettings settings, DictionaryStore store, Action applySettings,
        Action dictionaryChanged, Action backHome)
    {
        _ready = false;
        _settings=settings;
        _lexicon=new LexiconUpdateService(store);
        _applySettings=applySettings;
        _dictionaryChanged=dictionaryChanged;
        _backHome=backHome;
        ThemeBox.SelectedIndex=Math.Clamp(settings.Appearance,0,2);
        BackdropBox.SelectedIndex=Math.Clamp(settings.Backdrop,0,2);
        SearchStyleBox.SelectedIndex=Math.Clamp(settings.SearchStyle,0,2);
        DefinitionSwitch.IsOn=settings.ShowEnglishDefinition;
        GlossarySwitch.IsOn=settings.EnableGlossary;
        TtsSwitch.IsOn=settings.EnableTts;
        VolumeSlider.Value=settings.TtsVolume;
        ClearSwitch.IsOn=settings.AutoClearSearch;
        DatabaseVersionText.Text="本机词库："+_lexicon.InstalledVersion;
        _ready=true;
    }
    private void SavePreferences()
    {
        if(!_ready || _settings is null) return;
        _settings.Appearance=ThemeBox.SelectedIndex;
        _settings.Backdrop=BackdropBox.SelectedIndex;
        _settings.SearchStyle=SearchStyleBox.SelectedIndex;
        _settings.ShowEnglishDefinition=DefinitionSwitch.IsOn;
        _settings.EnableGlossary=GlossarySwitch.IsOn;
        _settings.EnableTts=TtsSwitch.IsOn;
        _settings.AutoClearSearch=ClearSwitch.IsOn;
        _settings.TtsVolume=(int)VolumeSlider.Value;
        _settings.Save();
        _applySettings?.Invoke();
    }
    private void OnPreferenceChanged(object sender, RoutedEventArgs e) => SavePreferences();
    private void OnPreferenceChanged(object sender, SelectionChangedEventArgs e) => SavePreferences();
    private void OnVolumeChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        => SavePreferences();

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

    private async void OnAppUpdateClicked(object sender,RoutedEventArgs e)
    {
        AppUpdateStatus.Text="正在检查软件版本…";
        try
        {
            var result=await AppUpdateService.CheckAsync();
            AppUpdateStatus.Text=result.Newer ? $"发现新版本 {result.Version}，即将打开发布页"
                : $"当前版本 0.1.2；{result.Version}。";
            if(result.Newer) await Launcher.LaunchUriAsync(new Uri(result.Url));
        }
        catch(Exception ex){AppUpdateStatus.Text="检查失败："+ex.Message;}
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
                DatabaseUpdateStatus.Text=$"已是最新词库 {available.Version}（{available.WordCount} 词）";
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
            DatabaseUpdateStatus.Text=$"数据库更新完成 · {available.WordCount} 词。返回主页即可查词。";
            _dictionaryChanged?.Invoke();
        }
        catch(Exception ex){DatabaseUpdateStatus.Text="数据库检查/更新失败："+ex.Message;}
        finally{DatabaseUpdateButton.IsEnabled=true;}
    }
}
