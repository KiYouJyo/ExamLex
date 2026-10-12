using System.Text.Json;
namespace ExamLex.Services;
public sealed class AppSettings
{
    public double MainWindowWidth { get; set; }=580;
    public double MainWindowHeight { get; set; }=386;
    public int Appearance { get; set; }=1; // System, Dark, Light
    public int Backdrop { get; set; }=1; // 1 Mica, 2 Acrylic
    public bool ShowEnglishDefinition { get; set; }=true;
    public bool EnableGlossary { get; set; }=true;
    public bool AutoClearSearch { get; set; }
    public int SearchStyle { get; set; }
    public bool EnableTts { get; set; }
    public int TtsVolume { get; set; }=80;
    public string TtsVoiceId { get; set; }="";
    public bool StartWithWindows { get; set; }
    public bool HotkeyEnabled { get; set; }
    public int HotkeyPreset { get; set; } // Ctrl+Alt+E / Ctrl+Shift+E / Alt+Shift+E
    public int HotkeyWindowMode { get; set; } // Full / Compact
    public int CloseMode { get; set; }=1; // 0 tray, 1 exit
    public static string FilePath=>Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ExamLex","appsettings.json");
    public static AppSettings Load()
    {
        try{return File.Exists(FilePath)?
            JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath))??new():new();}
        catch(Exception ex)when(ex is IOException or JsonException){return new();}
    }
    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        string temp=FilePath+".tmp";
        File.WriteAllText(temp,JsonSerializer.Serialize(this,new JsonSerializerOptions{WriteIndented=true}));
        File.Move(temp,FilePath,true);
    }
}
