using ExamLex.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.System;
using System.Diagnostics;
using Windows.Media.SpeechSynthesis;
using Windows.Media.Core;
using Windows.Media.Playback;

namespace ExamLex.Views;

public sealed partial class SettingsPage : Page
{
    private AppSettings? _settings;
    private LexiconUpdateService? _lexicon;
    private Action? _applySettings;
    private Action? _dictionaryChanged;
    private Action? _backHome;
    private Action? _exitApplication;
    private bool _ready;
    private bool _busy;
    private string? _pendingAppUpdate;
    private bool _pendingLexiconRestart;
    private readonly List<VoiceInformation> _voices=new();

    public SettingsPage()
    {
        InitializeComponent();
        Loaded+=(_,_)=>DispatcherQueue.TryEnqueue(()=>
            SettingsScroll.ChangeView(null,0,null,true));
        MicaSurfaceService.Attach(this);
    }

    public void Initialize(AppSettings settings,DictionaryStore store,Action applySettings,
        Action dictionaryChanged,Action backHome,Action exitApplication)
    {
        _ready=false;
        _settings=settings;
        _lexicon=new LexiconUpdateService(store);
        _applySettings=applySettings;
        _dictionaryChanged=dictionaryChanged;
        _backHome=backHome;
        _exitApplication=exitApplication;
        var version=typeof(SettingsPage).Assembly.GetName().Version;
        AppVersionText.Text=version is null?"ExamLex":
            $"ExamLex {version.Major}.{version.Minor}.{version.Build}";
        ThemeBox.SelectedIndex=Math.Clamp(settings.Appearance,0,2);
        BackdropBox.SelectedIndex=settings.Backdrop==2?1:0;
        switch(Math.Clamp(settings.SearchStyle,0,2))
        {
            case 0:ModernStyleRadio.IsChecked=true;break;
            case 1:RoundStyleRadio.IsChecked=true;break;
            default:ClassicStyleRadio.IsChecked=true;break;
        }
        DefinitionSwitch.IsOn=settings.ShowEnglishDefinition;
        EnableGlossaryToggleSwitch.IsOn=settings.EnableGlossary;
        EnableTTSToggleSwitch.IsOn=settings.EnableTts;
        VolumeSlider.Value=Math.Clamp(settings.TtsVolume/10d,1,10);
        ClearSwitch.IsOn=settings.AutoClearSearch;
        StartupSettingToggleSwitch.IsOn=WindowIntegrationService.IsStartupEnabled();
        HotkeyToggle.IsOn=settings.HotkeyEnabled;
        HotkeyPresetBox.SelectedIndex=Math.Clamp(settings.HotkeyPreset,0,2);
        WindowModeComboBox.SelectedIndex=Math.Clamp(settings.HotkeyWindowMode,0,1);
        CloseModeComboBox.SelectedIndex=Math.Clamp(settings.CloseMode,0,1);
        _voices.Clear();
        TTSVoicesComboBox.Items.Clear();
        foreach(var voice in SpeechSynthesizer.AllVoices)
        {
            _voices.Add(voice);
            TTSVoicesComboBox.Items.Add(voice.DisplayName+" ("+voice.Language+")");
        }
        int voiceIndex=_voices.FindIndex(v=>v.Id==settings.TtsVoiceId);
        TTSVoicesComboBox.SelectedIndex=voiceIndex>=0?voiceIndex:(_voices.Count>0?0:-1);
        DatabaseVersionText.Text="本机词库："+_lexicon.InstalledVersion;
        DatabaseVersionText.Visibility=Visibility.Visible;
        _ready=true;
    }

    private void SavePreferences()
    {
        if(!_ready||_settings is null)return;
        var st=_settings;
        st.Appearance=ThemeBox.SelectedIndex;
        st.Backdrop=BackdropBox.SelectedIndex==1?2:1;
        st.SearchStyle=ModernStyleRadio.IsChecked==true?0:
            RoundStyleRadio.IsChecked==true?1:2;
        st.ShowEnglishDefinition=DefinitionSwitch.IsOn;
        st.EnableGlossary=EnableGlossaryToggleSwitch.IsOn;
        st.EnableTts=EnableTTSToggleSwitch.IsOn;
        st.AutoClearSearch=ClearSwitch.IsOn;
        st.TtsVolume=(int)Math.Round(VolumeSlider.Value*10);
        st.HotkeyEnabled=HotkeyToggle.IsOn;
        st.HotkeyPreset=Math.Max(0,HotkeyPresetBox.SelectedIndex);
        st.HotkeyWindowMode=Math.Max(0,WindowModeComboBox.SelectedIndex);
        st.CloseMode=Math.Max(0,CloseModeComboBox.SelectedIndex);
        st.TtsVoiceId=TTSVoicesComboBox.SelectedIndex>=0 &&
            TTSVoicesComboBox.SelectedIndex<_voices.Count?
            _voices[TTSVoicesComboBox.SelectedIndex].Id:"";
        try
        {
            if(WindowIntegrationService.IsStartupEnabled()!=StartupSettingToggleSwitch.IsOn)
                WindowIntegrationService.SetStartup(StartupSettingToggleSwitch.IsOn);
            st.StartWithWindows=StartupSettingToggleSwitch.IsOn;
            StartupDescTextBlock.Text="登录 Windows 时自动启动 ExamLex";
        }
        catch(Exception ex)
        {
            StartupDescTextBlock.Text="开机自启设置失败："+ex.Message;
            st.StartWithWindows=WindowIntegrationService.IsStartupEnabled();
        }
        st.Save();
        _applySettings?.Invoke();
    }
    private void OnPreferenceChanged(object sender,RoutedEventArgs e)=>SavePreferences();
    private void OnPreferenceChanged(object sender,SelectionChangedEventArgs e)=>SavePreferences();
    private void OnVolumeChanged(object sender,RangeBaseValueChangedEventArgs e)=>SavePreferences();
    private void OnSearchStyleChecked(object sender,RoutedEventArgs e)=>SavePreferences();

    private void OnSettingsScrollChanged(object sender,ScrollViewerViewChangedEventArgs e)
    {
        if(sender is ScrollViewer viewer)
        {
            double maximum=Math.Min(160,viewer.ScrollableHeight);
            if(maximum<=0)return;
            double opacity=Math.Clamp(viewer.VerticalOffset/maximum,0,1);
            PageDragRegion.Opacity=opacity;
            SettingsPageHeaderSeperatorLineBorder.Opacity=opacity;
        }
    }
    private void OnVoiceChanged(object sender,SelectionChangedEventArgs e)=>SavePreferences();

    private async void OnHearVoiceClicked(object sender,RoutedEventArgs e)
    {
        try
        {
            using var synth=new SpeechSynthesizer();
            if(_settings is not null)
            {
                var selected=_voices.FirstOrDefault(v=>v.Id==_settings.TtsVoiceId);
                if(selected is not null)synth.Voice=selected;
            }
            var stream=await synth.SynthesizeTextToStreamAsync("ExamLex");
            using var player=new MediaPlayer();
            player.Source=MediaSource.CreateFromStream(stream,stream.ContentType);
            player.Volume=(_settings?.TtsVolume??80)/100d;
            player.Play();
            await Task.Delay(1600);
        }
        catch(Exception ex){StartupDescTextBlock.Text="声音测试失败："+ex.Message;}
    }

    private void OnBackClicked(object sender,RoutedEventArgs e)=>_backHome?.Invoke();

    private void OnGlossaryFolderClicked(object sender,RoutedEventArgs e)
    {
        string directory=Path.GetDirectoryName(AppSettings.FilePath)!;
        Directory.CreateDirectory(directory);
        Process.Start(new ProcessStartInfo {
            FileName="explorer.exe",Arguments="\""+directory+"\"",
            UseShellExecute=true
        });
    }
    private async void OnSourceClicked(object sender,RoutedEventArgs e)=>
        await Launcher.LaunchUriAsync(new Uri("https://github.com/KiYouJyo/ExamLex"));

    private void Busy(bool app,bool state)
    {
        if(app)
        {
            AppUpdateRing.IsActive=state;
            AppUpdateRing.Visibility=state?Visibility.Visible:Visibility.Collapsed;
            AppUpdateGlyph.Visibility=state?Visibility.Collapsed:Visibility.Visible;
            AppUpdateButton.IsEnabled=!state;
        }
        else
        {
            DatabaseUpdateRing.IsActive=state;
            DatabaseUpdateRing.Visibility=state?Visibility.Visible:Visibility.Collapsed;
            DatabaseUpdateGlyph.Visibility=state?Visibility.Collapsed:Visibility.Visible;
            DatabaseUpdateButton.IsEnabled=!state;
        }
    }
    private void Status(bool app,string message)
    {
        var label=app?AppUpdateStatus:DatabaseUpdateStatus;
        label.Visibility=Visibility.Visible;
        label.Text=message;
    }

    private async void OnAppUpdateClicked(object sender,RoutedEventArgs e)
    {
        if(_pendingAppUpdate is not null)
        {
            try{AppUpdateService.Restart(true,_pendingAppUpdate);_exitApplication?.Invoke();}
            catch(Exception ex){Status(true,"安装更新失败："+ex.Message);}
            return;
        }
        Busy(true,true);
        AppUpdateText.Text="检查更新中";
        try
        {
            var release=await AppUpdateService.CheckAsync();
            if(!release.Newer)
            {
                Status(true,release.Version=="尚未发布正式版本"?
                    "尚未发布正式版本，当前安装包无需更新。":"当前已是最新版本。");
                return;
            }
            if(release.PackageUrl is null){Status(true,"该版本未提供已签名 x64 MSIX 下载。");return;}
            AppUpdateText.Text="下载更新中";
            Status(true,"正在获取软件 "+release.Version+" 的已签名安装包…");
            _pendingAppUpdate=await AppUpdateService.DownloadAsync(release);
            AppUpdateText.Text="重启应用更新";
            Status(true,"软件更新已下载并校验，点击按钮安装并重启。");
        }
        catch(Exception ex){Status(true,"检查或下载失败："+ex.Message);}
        finally
        {
            Busy(true,false);
            if(_pendingAppUpdate is null)AppUpdateText.Text="检查更新";
        }
    }

    private async void OnDatabaseUpdateClicked(object sender,RoutedEventArgs e)
    {
        if(_pendingLexiconRestart)
        {
            try{AppUpdateService.Restart(false);_exitApplication?.Invoke();}
            catch(Exception ex){Status(false,"重启失败："+ex.Message);}
            return;
        }
        if(_lexicon is null)return;
        Busy(false,true);
        DatabaseUpdateText.Text="检查更新中";
        try
        {
            var info=await _lexicon.CheckAsync();
            if(!info.UpdateAvailable)
            {
                Status(false,"当前词库已是最新版本："+info.Version);
                return;
            }
            DatabaseUpdateText.Text="下载更新中";
            Status(false,"正在下载并校验数据库："+info.Version);
            await _lexicon.InstallAsync(info);
            DatabaseVersionText.Text="本机词库："+_lexicon.InstalledVersion;
            _dictionaryChanged?.Invoke();
            _pendingLexiconRestart=true;
            DatabaseUpdateText.Text="重启应用更新";
            Status(false,"词库已安装，点击按钮重启并加载新数据库。");
        }
        catch(Exception ex){Status(false,"检查或下载失败："+ex.Message);}
        finally
        {
            Busy(false,false);
            if(!_pendingLexiconRestart)DatabaseUpdateText.Text="检查更新";
        }
    }
}
