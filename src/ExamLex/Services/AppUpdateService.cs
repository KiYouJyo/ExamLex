using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Xml;

namespace ExamLex.Services;

public sealed record AppUpdateInfo(string Version,string ReleaseUrl,string? PackageUrl,bool Newer);

public static class AppUpdateService
{
    private const string Api="https://api.github.com/repos/KiYouJyo/ExamLex/releases/latest";
    private const long Limit=300L*1024*1024;
    private static readonly HttpClient Http=new(){Timeout=TimeSpan.FromMinutes(8)};
    static AppUpdateService()
    {
        Http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("ExamLex","0.1.8"));
        Http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
    }

    public static async Task<AppUpdateInfo> CheckAsync(CancellationToken ct=default)
    {
        using var response=await Http.GetAsync(Api,ct);
        if(response.StatusCode==System.Net.HttpStatusCode.NotFound)
            return new("尚未发布正式版本","https://github.com/KiYouJyo/ExamLex/releases",null,false);
        response.EnsureSuccessStatusCode();
        await using var stream=await response.Content.ReadAsStreamAsync(ct);
        using var json=await JsonDocument.ParseAsync(stream,cancellationToken:ct);
        var r=json.RootElement;
        string ver=r.GetProperty("tag_name").GetString() ?? "";
        string page=r.GetProperty("html_url").GetString() ?? "https://github.com/KiYouJyo/ExamLex/releases";
        string? msix=null;
        foreach(var asset in r.GetProperty("assets").EnumerateArray())
        {
            string n=asset.GetProperty("name").GetString()??"";
            if(n.EndsWith("_x64.msix",StringComparison.OrdinalIgnoreCase))
            {msix=asset.GetProperty("browser_download_url").GetString();break;}
        }
        bool newer=Version.TryParse(ver.TrimStart('v','V'),out var remote)&&
            remote>(typeof(AppUpdateService).Assembly.GetName().Version ?? new Version(0,1,0));
        return new(ver,page,msix,newer);
    }

    public static async Task<string> DownloadAsync(AppUpdateInfo info,CancellationToken ct=default)
    {
        if(!info.Newer||info.PackageUrl is null)
            throw new InvalidOperationException("最新发布版本没有可安装的 x64 MSIX。");
        Uri uri=new(info.PackageUrl);
        if(uri.Scheme!="https"||uri.Host!="github.com"||
           !uri.AbsolutePath.StartsWith("/KiYouJyo/ExamLex/releases/download/",StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("软件更新来源不受信任。");
        string dir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"ExamLex","updates");
        Directory.CreateDirectory(dir);
        string file=Path.Combine(dir,"ExamLex-"+Guid.NewGuid().ToString("N")+".msix");
        try
        {
            using var response=await Http.GetAsync(uri,HttpCompletionOption.ResponseHeadersRead,ct);
            response.EnsureSuccessStatusCode();
            if(response.Content.Headers.ContentLength is long len&&len>Limit)throw new InvalidDataException("安装包超过大小限制。");
            await using(var input=await response.Content.ReadAsStreamAsync(ct))
            await using(var output=new FileStream(file,FileMode.CreateNew,FileAccess.Write,FileShare.None,81920,true))
            {
                byte[] data=new byte[81920];long count=0;int n;
                while((n=await input.ReadAsync(data,ct))>0)
                {
                    if((count+=n)>Limit)throw new InvalidDataException("安装包超过大小限制。");
                    await output.WriteAsync(data.AsMemory(0,n),ct);
                }
            }
            using var zip=ZipFile.OpenRead(file);
            using var stream=(zip.GetEntry("AppxManifest.xml")??throw new InvalidDataException("缺少 MSIX Manifest。")).Open();
            using var reader=XmlReader.Create(stream,new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null});
            var doc=new XmlDocument{XmlResolver=null};doc.Load(reader);
            var id=doc.GetElementsByTagName("Identity").OfType<XmlElement>().FirstOrDefault()
                ??throw new InvalidDataException("缺少 MSIX 身份。");
            var current=Windows.ApplicationModel.Package.Current.Id;
            if(id.GetAttribute("Name")!="KiYouJyo.ExamLex"||id.GetAttribute("Publisher")!=current.Publisher||
               !Version.TryParse(id.GetAttribute("Version"),out var target)||
               target<=(typeof(AppUpdateService).Assembly.GetName().Version??new Version(0,1,0))||
               zip.GetEntry("AppxSignature.p7x") is null)
                throw new InvalidDataException("安装包版本、签名或发布者身份不匹配。");
            return file;
        }
        catch {if(File.Exists(file))File.Delete(file);throw;}
    }

    public static void Restart(bool update,string? file=null)
    {
        if(update&&(!File.Exists(file)||file is null))throw new FileNotFoundException("更新包不存在。",file);
        string family=Windows.ApplicationModel.Package.Current.Id.FamilyName;
        string? path=file?.Replace("'","''");
        string script=update
            ? "$ErrorActionPreference='Stop'; try { "+
              "Wait-Process -Id "+Environment.ProcessId+" -Timeout 30 -ErrorAction SilentlyContinue; "+
              "Start-Sleep -Milliseconds 800; Add-AppxPackage -Path '"+path+"' -ErrorAction Stop; "+
              "Start-Process explorer.exe -ArgumentList 'shell:AppsFolder\\"
              +family+"!App' } catch { $_.Exception.ToString() | Out-File '"+path+".log' }"
            : "Start-Sleep -Seconds 2; Start-Process explorer.exe -ArgumentList 'shell:AppsFolder\\"
              +family+"!App'";
        string encoded=Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
        Process.Start(new ProcessStartInfo {
            FileName="powershell.exe",
            Arguments="-NoProfile -NonInteractive -WindowStyle Hidden -EncodedCommand "+encoded,
            CreateNoWindow=true,UseShellExecute=false
        })??throw new InvalidOperationException("无法重启更新程序。");
    }
}
