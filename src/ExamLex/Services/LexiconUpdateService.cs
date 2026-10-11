using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;

namespace ExamLex.Services;

public sealed record LexiconUpdateInfo(string Version, string Description, bool UpdateAvailable,
    string DownloadUrl, string Sha256, int WordCount);

/// <summary>Fetches a versioned release, verifies SHA256/SQLite, and atomically updates the local database.</summary>
public sealed class LexiconUpdateService
{
    private const string Api = "https://api.github.com/repos/KiYouJyo/ExamLex-Lexicon/releases/latest";
    private const long Limit = 200L * 1024 * 1024;
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromMinutes(4) };
    private readonly DictionaryStore _store;
    private string StateFile => Path.Combine(_store.DataDirectory, "lexicon-state.json");
    static LexiconUpdateService()
    {
        Http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("ExamLex","0.1.1"));
        Http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
    }
    public LexiconUpdateService(DictionaryStore store) => _store = store;
    public string InstalledVersion
    {
        get
        {
            if (!File.Exists(_store.DatabasePath)) return "未安装";
            try
            {
                using var state = JsonDocument.Parse(File.ReadAllText(StateFile));
                return state.RootElement.GetProperty("version").GetString() ?? "未知";
            }
            catch (Exception e) when (e is IOException or JsonException or KeyNotFoundException)
            { return "未知"; }
        }
    }

    public async Task<LexiconUpdateInfo> CheckAsync(CancellationToken ct = default)
    {
        using var release = await GetJsonAsync(Api,ct);
        var root = release.RootElement;
        string version = root.GetProperty("tag_name").GetString()
            ?? throw new InvalidDataException("发布信息缺少版本");
        string manifestUrl = AssetUrl(root,"lexicon-manifest.json")
            ?? throw new InvalidDataException("没有公开词库校验清单");
        string downloadUrl = AssetUrl(root,"examlex-lexicon.db")
            ?? throw new InvalidDataException("没有公开 SQLite 数据库");
        using var metadata = await GetJsonAsync(manifestUrl,ct);
        var m = metadata.RootElement;
        if (m.GetProperty("schema_version").GetInt32()!=1 ||
            m.GetProperty("version").GetString()!=version ||
            m.GetProperty("filename").GetString()!="examlex-lexicon.db")
            throw new InvalidDataException("词库版本与 Schema 不匹配");
        string sha = m.GetProperty("sha256").GetString() ?? "";
        if (sha.Length!=64 || !sha.All(Uri.IsHexDigit)) throw new InvalidDataException("SHA256 格式错误");
        return new(version,m.GetProperty("description").GetString() ?? "",
            !File.Exists(_store.DatabasePath) || InstalledVersion!=version,
            downloadUrl,sha,m.GetProperty("entry_count").GetInt32());
    }

    public async Task InstallAsync(LexiconUpdateInfo info, CancellationToken ct=default)
    {
        var path = Path.Combine(_store.DataDirectory, "download-" + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            ValidateUri(info.DownloadUrl);
            using var response = await Http.GetAsync(info.DownloadUrl,HttpCompletionOption.ResponseHeadersRead,ct);
            response.EnsureSuccessStatusCode();
            if(response.Content.Headers.ContentLength is long contentSize && contentSize>Limit)
                throw new InvalidDataException("词库文件超过 200 MB 上限");
            await using(var source = await response.Content.ReadAsStreamAsync(ct))
            await using(var output = new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None,81920,true))
            {
                var buffer = new byte[81920];long total = 0;
                while(true)
                {
                    int n=await source.ReadAsync(buffer,ct);
                    if(n==0) break;
                    total+=n;
                    if(total>Limit) throw new InvalidDataException("词库文件超出 200 MB 上限");
                    await output.WriteAsync(buffer.AsMemory(0,n),ct);
                }
            }
            await using(var file=File.OpenRead(path))
            {
                string sum=Convert.ToHexString(await SHA256.HashDataAsync(file,ct));
                if(!sum.Equals(info.Sha256,StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("SHA256 不匹配，已阻止更新");
            }
            DictionaryStore.ValidateCanonicalSchema(path);
            File.Move(path,_store.DatabasePath,true);
            File.WriteAllText(StateFile,JsonSerializer.Serialize(new{
                version=info.Version,sha256=info.Sha256,updated_at=DateTimeOffset.UtcNow
            }));
        }
        finally {if(File.Exists(path)) File.Delete(path);}
    }
    private static async Task<JsonDocument> GetJsonAsync(string url,CancellationToken ct)
    {
        ValidateUri(url);
        using var response=await Http.GetAsync(url,ct);
        response.EnsureSuccessStatusCode();
        await using var stream=await response.Content.ReadAsStreamAsync(ct);
        return await JsonDocument.ParseAsync(stream,cancellationToken:ct);
    }
    private static string? AssetUrl(JsonElement root,string name)
    {
        foreach(var asset in root.GetProperty("assets").EnumerateArray())
            if(asset.GetProperty("name").GetString()==name)
                return asset.GetProperty("browser_download_url").GetString();
        return null;
    }
    private static void ValidateUri(string url)
    {
        if(!Uri.TryCreate(url,UriKind.Absolute,out var uri) || uri.Scheme!="https" ||
           !(uri.Host=="github.com" || uri.Host=="api.github.com") ||
           !uri.AbsolutePath.Contains("KiYouJyo/ExamLex-Lexicon",StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("词库地址不属于官方 ExamLex-Lexicon 仓库");
    }
}
