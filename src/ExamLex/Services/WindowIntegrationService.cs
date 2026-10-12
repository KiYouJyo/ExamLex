using System.Runtime.InteropServices;
using Microsoft.Win32;
using Microsoft.UI.Xaml;
using Windows.ApplicationModel;
using WinRT.Interop;

namespace ExamLex.Services;

/// <summary>Native Windows global hotkeys, startup and shell notification-area integration.</summary>
public sealed class WindowIntegrationService : IDisposable
{
    private const string RunKey=@"Software\Microsoft\Windows\CurrentVersion\Run";
    private const uint WmHotkey=0x0312, TrayMessage=0x8000+77;
    private const uint WmLeftDoubleClick=0x0203,WmRightButtonUp=0x0205;
    private const int HotkeyId=0x4558,TrayIconId=0x4559;
    private readonly MainWindow _window;
    private readonly AppSettings _settings;
    private readonly IntPtr _hwnd;
    private readonly SubclassProc _proc;
    private bool _hooked,_registered,_trayAdded,_disposed;
    private IntPtr _icon=IntPtr.Zero;
    public string HotkeyStatus { get; private set; }="";

    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]
    private struct NotifyIconData
    {
        public int cbSize;
        public IntPtr hWnd;
        public uint uID,uFlags,uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr,SizeConst=128)]
        public string szTip;
        public uint dwState,dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr,SizeConst=256)]
        public string szInfo;
        public uint uTimeoutOrVersion;
        [MarshalAs(UnmanagedType.ByValTStr,SizeConst=64)]
        public string szInfoTitle;
        public uint dwInfoFlags;
        public Guid guidItem;
        public IntPtr hBalloonIcon;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct Point{public int X,Y;}
    private delegate IntPtr SubclassProc(IntPtr h,uint m,IntPtr w,IntPtr l,UIntPtr id,UIntPtr data);
    [DllImport("comctl32.dll",SetLastError=true)][return:MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowSubclass(IntPtr h,SubclassProc p,UIntPtr id,UIntPtr data);
    [DllImport("comctl32.dll")][return:MarshalAs(UnmanagedType.Bool)]
    private static extern bool RemoveWindowSubclass(IntPtr h,SubclassProc p,UIntPtr id);
    [DllImport("comctl32.dll")]
    private static extern IntPtr DefSubclassProc(IntPtr h,uint m,IntPtr w,IntPtr l);
    [DllImport("user32.dll",SetLastError=true)][return:MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(IntPtr h,int id,uint mod,uint key);
    [DllImport("user32.dll")][return:MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(IntPtr h,int id);
    [DllImport("user32.dll")][return:MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(IntPtr h,int command);
    [DllImport("user32.dll")][return:MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]
    private static extern IntPtr LoadImage(IntPtr instance,string path,uint type,int cx,int cy,uint flags);
    [DllImport("user32.dll")][return:MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr icon);
    [DllImport("shell32.dll",CharSet=CharSet.Unicode)]
    [return:MarshalAs(UnmanagedType.Bool)]
    private static extern bool Shell_NotifyIcon(uint message,ref NotifyIconData data);
    [DllImport("user32.dll")]
    private static extern IntPtr CreatePopupMenu();
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]
    [return:MarshalAs(UnmanagedType.Bool)]
    private static extern bool AppendMenu(IntPtr menu,uint flags,uint id,string label);
    [DllImport("user32.dll")]
    private static extern uint TrackPopupMenu(IntPtr menu,uint flags,int x,int y,int reserved,IntPtr window,IntPtr rect);
    [DllImport("user32.dll")][return:MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyMenu(IntPtr menu);
    [DllImport("user32.dll")][return:MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out Point position);

    public WindowIntegrationService(MainWindow window,AppSettings settings)
    {
        _window=window;_settings=settings;
        _hwnd=WindowNative.GetWindowHandle(window);
        _proc=OnWindowMessage;
        _hooked=SetWindowSubclass(_hwnd,_proc,(UIntPtr)HotkeyId,UIntPtr.Zero);
        if(!_hooked)HotkeyStatus="系统快捷键消息注册失败。";
        Apply();
    }
    public static bool IsStartupEnabled()
    {
        using var key=Registry.CurrentUser.OpenSubKey(RunKey);
        return key?.GetValue("ExamLex") is string command&&
            command.Contains(Package.Current.Id.FamilyName,StringComparison.OrdinalIgnoreCase);
    }
    public static void SetStartup(bool enabled)
    {
        using var key=Registry.CurrentUser.CreateSubKey(RunKey);
        if(key is null)throw new IOException("当前用户启动项不可写。");
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
            uint modifiers=_settings.HotkeyPreset switch {
                1=>0x0002u|0x0004u,2=>0x0001u|0x0004u,_=>0x0002u|0x0001u
            };
            _registered=RegisterHotKey(_hwnd,HotkeyId,modifiers,0x45);
            if(!_registered)HotkeyStatus="快捷键已被占用，请选择其他组合。";
        }
        ConfigureTray();
    }
    private NotifyIconData NewTrayData()=>new()
    {
        cbSize=Marshal.SizeOf<NotifyIconData>(),
        hWnd=_hwnd,uID=TrayIconId,uFlags=1|2|4,
        uCallbackMessage=TrayMessage,
        hIcon=_icon,szTip="ExamLex",szInfo="",szInfoTitle=""
    };
    private void ConfigureTray()
    {
        if(_settings.CloseMode==0 && !_trayAdded)
        {
            if(_icon==IntPtr.Zero)
            {
                string p=Path.Combine(AppContext.BaseDirectory,"Assets","ExamLex.ico");
                if(File.Exists(p))_icon=LoadImage(IntPtr.Zero,p,1,32,32,0x10);
            }
            var data=NewTrayData();
            _trayAdded=Shell_NotifyIcon(0,ref data);
        }
        else if(_settings.CloseMode!=0 && _trayAdded)
        {
            var data=NewTrayData();Shell_NotifyIcon(2,ref data);_trayAdded=false;
        }
    }
    private IntPtr OnWindowMessage(IntPtr hwnd,uint msg,IntPtr wp,IntPtr lp,UIntPtr id,UIntPtr data)
    {
        if(msg==WmHotkey && wp.ToInt32()==HotkeyId)
        {
            Restore();
            _window.ActivateHotkeyMode(_settings.HotkeyWindowMode==1);
            return IntPtr.Zero;
        }
        if(msg==TrayMessage)
        {
            uint eventType=unchecked((uint)lp.ToInt64())&0xffff;
            if(eventType==WmLeftDoubleClick)Restore();
            else if(eventType==WmRightButtonUp)ShowTrayMenu();
            return IntPtr.Zero;
        }
        return DefSubclassProc(hwnd,msg,wp,lp);
    }
    private void ShowTrayMenu()
    {
        if(!GetCursorPos(out var pos))return;
        IntPtr menu=CreatePopupMenu();
        if(menu==IntPtr.Zero)return;
        try
        {
            AppendMenu(menu,0,1,"显示 ExamLex");
            AppendMenu(menu,0,2,"退出 ExamLex");
            SetForegroundWindow(_hwnd);
            uint chosen=TrackPopupMenu(menu,0x0100|0x0002,pos.X,pos.Y,0,_hwnd,IntPtr.Zero);
            if(chosen==1)Restore();
            if(chosen==2)_window.ExitFromTray();
        }
        finally{DestroyMenu(menu);}
    }
    public void HideToTray(){ConfigureTray();ShowWindow(_hwnd,0);}
    public void Restore()
    {
        ShowWindow(_hwnd,9);
        _window.Activate();
        SetForegroundWindow(_hwnd);
    }
    public void Dispose()
    {
        if(_disposed)return;
        _disposed=true;
        if(_registered)UnregisterHotKey(_hwnd,HotkeyId);
        if(_trayAdded){var d=NewTrayData();Shell_NotifyIcon(2,ref d);_trayAdded=false;}
        if(_icon!=IntPtr.Zero){DestroyIcon(_icon);_icon=IntPtr.Zero;}
        if(_hooked)RemoveWindowSubclass(_hwnd,_proc,(UIntPtr)HotkeyId);
    }
}
