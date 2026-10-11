using System.Net.Http.Headers;
using System.Text.Json;

namespace ExamLex.Services;

public static class AppUpdateService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(20) };
    static AppUpdateService() => Http.DefaultRequestHeaders.UserAgent.Add(
        new ProductInfoHeaderValue("ExamLex", "0.1.1"));

    public static async Task<(string Version,string Url,bool Newer)> CheckAsync()
    {
        using var response=await Http.GetAsync("https://api.github.com/repos/KiYouJyo/ExamLex/releases/latest");
        if(response.StatusCode==System.Net.HttpStatusCode.NotFound)
            return ("尚未发布正式版本","https://github.com/KiYouJyo/ExamLex/releases",false);
        response.EnsureSuccessStatusCode();
        await using var stream=await response.Content.ReadAsStreamAsync();
        using var doc=await JsonDocument.ParseAsync(stream);
        string version=doc.RootElement.GetProperty("tag_name").GetString() ?? "";
        string url=doc.RootElement.GetProperty("html_url").GetString() ?? "https://github.com/KiYouJyo/ExamLex/releases";
        return (version,url, Version.TryParse(version.TrimStart('v'),out var remote) &&
            remote>(typeof(AppUpdateService).Assembly.GetName().Version ?? new Version(0,1,0)));
    }
}
