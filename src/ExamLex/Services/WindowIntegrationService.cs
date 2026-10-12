using System.Runtime.InteropServices;
using Microsoft.Win32;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.ApplicationModel;
using WinRT.Interop;

namespace ExamLex.Services;

/// <summary>Actual per-user Windows startup, global hotkey and native tray lifecycle.</summary>
public sealed class WindowIntegrationService : IDisposable
{
    private const string RunKey=@"Software\Microsoft\Windows\CurrentVersion\Run";
    private const uint WM_HOTKEY=0x0312;
    private const int HotkeyId=0x4558;
    private readonly MainWindow _window;
    private readonly AppSettings _settings;
    private readonly IntPtr _hwnd;
    private readonly SubclassProc _proc;
    private bool _hooked;
    private bool _registered;
    private System.Windows.Forms.NotifyIcon? _tray;
    private System.Windows.Forms.ContextMenuStrip? _menu;
    private System.Drawing.Icon? _icon;
    private bool _disposed;
    public string HotkeyStatus { get; private set; }="";

    private delegate IntPtr SubclassProc(IntPtr hwnd,uint msg,IntPtr wp,IntPtr lp,UIntPtr id,UIntPtr data);
    [DllImport("comctl32.dll",EntryPoint="SetWindowSubclass",SetLastError=true)]
    [return:MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowSubclass(IntPtr hwnd,SubclassProc callback,UIntPtr id,UIntPtr data);
    [DllImport("comctl32.dll",EntryPoint="RemoveWindowSubclass")]
    private static extern bool RemoveWindowSubclass(IntPtr hwnd,SubclassProc callback,UIntPtr id);
    [DllImport("comctl32.dll",EntryPoint="DefSubclassProc")]
    private static extern IntPtr DefSubclassProc(IntPtr hwnd,uint msg,IntPtr wp,IntPtr lp);
    [DllImport("user32.dll",SetLastError=true)]
    [return:MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(IntPtr hwnd,int id,uint modifiers,uint key);
    [DllImport("user32.dll")]
    [return:MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(IntPtr hwnd,int id);
    [DllImport("user32.dll")]
    [return:MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(IntPtr hwnd,int cmd);
    [DllImport("user32.dll")]
    [return:MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hwnd);
    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);

    public WindowIntegrationService(MainWindow window,AppSettings settings)
    {
        _window=window;_settings=settings;
        _hwnd=WindowNative.GetWindowHandle(window);
        _proc=OnWindowMessage;
        _hooked=SetWindowSubclass(_hwnd,_proc,(UIntPtr)HotkeyId,UIntPtr.Zero);
        if(!_hooked)HotkeyStatus="无法初始化系统快捷键消息处理。";
        Apply();
    }
    public static bool IsStartupEnabled()
    {
        using var key=Registry.CurrentUser.OpenSubKey(RunKey);
        return key?.GetValue("ExamLex") is string command &&
            command.Contains(Package.Current.Id.FamilyName,StringComparison.OrdinalIgnoreCase);
    }
    public static void SetStartup(bool enabled)
    {
        using var key=Registry.CurrentUser.CreateSubKey(RunKey);
        if(key is null)throw new IOException("无法打开当前用户的启动项设置。");
        if(enabled)
            key.SetValue("ExamLex","explorer.exe shell:AppsFolder\\"+Package.Current.Id.FamilyName+"!App");
        else key.DeleteValue("ExamLex",false);
    }
    public void Apply()
    {
        if(_disposed)return;
        if(_registered){UnregisterHotKey(_hwnd,HotkeyId);_registered=false;}
        HotkeyStatus="";
        if(_settings.HotkeyEnabled)
        {
            uint mod=_settings.HotkeyPreset switch
            {
                1=>0x0002u|0x0004u, // Ctrl + Shift
                2=>0x0001u|0x0004u, // Alt + Shift
                _=>0x0002u|0x0001u // Ctrl + Alt
            };
            _registered=RegisterHotKey(_hwnd,HotkeyId,mod,0x45);
            if(!_registered)HotkeyStatus="快捷键已被其他应用占用，请换一个组合。";
        }
        ConfigureTray();
    }
    private void ConfigureTray()
    {
        if(_settings.CloseMode==0)
        {
            if(_tray is null)
            {
                string file=Path.Combine(AppContext.BaseDirectory,"Assets","ExamLex.ico");
                _icon=File.Exists(file)?new System.Drawing.Icon(file):null;
                _menu=new System.Windows.Forms.ContextMenuStrip();
                _menu.Items.Add("显示 ExamLex",null,(_,_)=>Restore());
                _menu.Items.Add("退出 ExamLex",null,(_,_)=>_window.ExitFromTray());
                _tray=new System.Windows.Forms.NotifyIcon
                {
                    Text="ExamLex",
                    Icon=_icon??System.Drawing.SystemIcons.Application,
                    ContextMenuStrip=_menu,
                    Visible=true
                };
                _tray.DoubleClick+=(_,_)=>Restore();
            }
            _tray.Visible=true;
        }
        else if(_tray is not null)_tray.Visible=false;
    }
    private IntPtr OnWindowMessage(IntPtr hwnd,uint msg,IntPtr wp,IntPtr lp,UIntPtr id,UIntPtr data)
    {
        if(msg==WM_HOTKEY && wp.ToInt32()==HotkeyId)
        {
            Restore();
            _window.ActivateHotkeyMode(_settings.HotkeyWindowMode==1);
            return IntPtr.Zero;
        }
        return DefSubclassProc(hwnd,msg,wp,lp);
    }
    public void HideToTray()
    {
        ConfigureTray();
        ShowWindow(_hwnd,0); // SW_HIDE; process remains alive and hotkey active.
    }
    public void Restore()
    {
        ShowWindow(_hwnd,9); // SW_RESTORE
        _window.Activate();
        SetForegroundWindow(_hwnd);
    }
    public void Dispose()
    {
        if(_disposed)return;
        _disposed=true;
        if(_registered)UnregisterHotKey(_hwnd,HotkeyId);
        if(_hooked)RemoveWindowSubclass(_hwnd,_proc,(UIntPtr)HotkeyId);
        if(_tray is not null){_tray.Visible=false;_tray.Dispose();}
        _menu?.Dispose();
        _icon?.Dispose();
    }
}
