using System.Text.Json;
namespace ExamLex.Services;
public sealed class AppSettings
{
    public int Appearance { get; set; } = 1; // System, Dark, Light
    public int Backdrop { get; set; } = 0; // Solid, Mica, Acrylic
    public bool ShowEnglishDefinition { get; set; } = true;
    public bool EnableGlossary { get; set; } = true;
    public bool AutoClearSearch { get; set; }
    public int SearchStyle { get; set; }
    public bool EnableTts { get; set; }
    public int TtsVolume { get; set; } = 80;
    public static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ExamLex", "appsettings.json");
    public static AppSettings Load()
    {
        try { return File.Exists(FilePath) ?
            JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new() : new(); }
        catch (Exception e) when (e is IOException or JsonException) { return new(); }
    }
    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        string staging = FilePath + ".tmp";
        File.WriteAllText(staging, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(staging, FilePath, true);
    }
}
