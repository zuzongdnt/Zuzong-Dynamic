using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;

namespace VNotch.Services;

internal sealed class ZuzongUpdateService : IUpdateService, IDisposable
{
    internal const string Repository = "zuzongdnt/Zuzong-Dynamic";
    internal const string RepositoryUrl = "https://github.com/" + Repository;
    private const string Feed = "https://api.github.com/repos/" + Repository + "/releases?per_page=50";
    private const long MaxInstallerBytes = 512 * 1024 * 1024;
    private readonly HttpClient _client;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Func<bool> _networkAllowed;
    private readonly Func<string, string, string, Task> _install;
    private IReadOnlyList<UpdateInfo>? _releases;
    private DateTime _checkedAt;

    public ZuzongUpdateService() : this(
        new HttpClient(NetworkPrivacy.Handler(NetworkFeature.Updates, new HttpClientHandler { AllowAutoRedirect = false }))
            { Timeout = TimeSpan.FromMinutes(10) },
        () => NetworkPrivacy.Current.IsAllowed(NetworkFeature.Updates), StartInstallerAsync) { }

    internal ZuzongUpdateService(HttpClient client, Func<bool> networkAllowed, Func<string, string, string, Task> install)
    {
        _client = client;
        _networkAllowed = networkAllowed;
        _install = install;
        _client.DefaultRequestHeaders.UserAgent.ParseAdd("Zuzong-Dynamic-Updater/" + CurrentVersion);
        _client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
    }

    public string CurrentVersion => AppVersion.Current;
    public UpdateInfo? LatestUpdateInfo { get; private set; }
    public event EventHandler<UpdateInfo?>? UpdateCheckCompleted;

    public async Task<UpdateInfo?> CheckForUpdatesAsync(bool includePrereleases = false, bool forceRefresh = false)
    {
        var releases = await FetchReleasesAsync(forceRefresh).ConfigureAwait(false);
        LatestUpdateInfo = releases.FirstOrDefault(r => includePrereleases || !r.IsPrerelease);
        foreach (var handler in UpdateCheckCompleted?.GetInvocationList() ?? [])
        {
            try { ((EventHandler<UpdateInfo?>)handler)(this, LatestUpdateInfo); }
            catch (Exception ex) { RuntimeLog.Error("UPDATE", ex, "Update callback failed"); }
        }
        return LatestUpdateInfo;
    }

    public Task<IReadOnlyList<UpdateInfo>> GetAllReleasesAsync() => FetchReleasesAsync(false);

    private async Task<IReadOnlyList<UpdateInfo>> FetchReleasesAsync(bool force)
    {
        EnsureNetwork();
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (!force && _releases != null && DateTime.UtcNow - _checkedAt < TimeSpan.FromMinutes(30)) return _releases;
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(25));
            using var response = await GetAsync(new Uri(Feed), false, deadline.Token).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.NotFound)
                throw new HttpRequestException("Chưa có kho phát hành công khai của Zuzong Dynamic trên GitHub.");
            if (response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.TooManyRequests)
                throw new HttpRequestException("GitHub đang giới hạn lượt kiểm tra. Vui lòng thử lại sau.");
            response.EnsureSuccessStatusCode();
            var bytes = await ReadLimitedAsync(response.Content, 2 * 1024 * 1024, deadline.Token).ConfigureAwait(false);
            _releases = ParseReleases(bytes, CurrentVersion);
            _checkedAt = DateTime.UtcNow;
            return _releases;
        }
        finally { _gate.Release(); }
    }

    internal static IReadOnlyList<UpdateInfo> ParseReleases(byte[] bytes, string currentVersion)
    {
        using var document = JsonDocument.Parse(bytes);
        var releases = new List<UpdateInfo>();
        foreach (var release in document.RootElement.EnumerateArray())
        {
            if (release.GetProperty("draft").GetBoolean()) continue;
            string tag = release.GetProperty("tag_name").GetString() ?? "";
            if (!Regex.IsMatch(tag, @"^v?\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?$")) continue;
            string version = tag.TrimStart('v');
            string name = $"ZuzongDynamic-Setup-{version}-x64.exe";
            var installer = release.GetProperty("assets").EnumerateArray()
                .FirstOrDefault(a => a.GetProperty("name").GetString() == name);
            if (installer.ValueKind == JsonValueKind.Undefined) continue;
            string url = installer.GetProperty("browser_download_url").GetString() ?? "";
            if (!IsReleaseAssetUrl(url, tag, name)) continue;
            string digest = installer.TryGetProperty("digest", out var d) ? d.GetString() ?? "" : "";
            string hash = digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase) ? digest[7..] : "";
            if (!Regex.IsMatch(hash, "^[a-fA-F0-9]{64}$")) continue;
            releases.Add(new UpdateInfo
            {
                Version = version, InstallerName = name, DownloadUrl = url, Sha256 = hash,
                ReleaseNotes = release.TryGetProperty("body", out var body) ? body.GetString() ?? "" : "",
                PublishedAt = release.TryGetProperty("published_at", out var at) && at.TryGetDateTime(out var time) ? time : DateTime.MinValue,
                IsPrerelease = release.GetProperty("prerelease").GetBoolean() || version.Contains('-'),
                IsNewerVersion = UpdateService.CompareVersions(version, currentVersion) > 0
            });
        }
        releases.Sort((a, b) => UpdateService.CompareVersions(b.Version, a.Version));
        return releases;
    }

    internal static bool IsReleaseAssetUrl(string url, string tag, string name) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == "https" && uri.IsDefaultPort &&
        uri.UserInfo.Length == 0 && uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) &&
        uri.Query.Length == 0 && uri.Fragment.Length == 0 &&
        uri.AbsolutePath == $"/{Repository}/releases/download/{tag}/{name}";

    internal async Task<string> DownloadVerifiedAsync(UpdateInfo requested, IProgress<double>? progress, CancellationToken token)
    {
        var releases = await FetchReleasesAsync(true).ConfigureAwait(false);
        var update = releases.FirstOrDefault(r => r.Version == requested.Version && r.DownloadUrl == requested.DownloadUrl);
        if (update == null || !update.IsNewerVersion) throw new InvalidDataException("Bản cập nhật không hợp lệ hoặc không mới hơn bản đang dùng.");
        string directory = Path.Combine(ZuzongIntegration.ParentData, "Updates", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, update.InstallerName);
        using var response = await GetAsync(new Uri(update.DownloadUrl), true, token).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        long? total = response.Content.Headers.ContentLength;
        if (total > MaxInstallerBytes) throw new InvalidDataException("Bộ cài vượt quá giới hạn dung lượng.");
        await using var input = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
        await using (var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 65536, true))
        {
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            byte[] buffer = new byte[65536];
            long received = 0;
            int count;
            while ((count = await input.ReadAsync(buffer, token).ConfigureAwait(false)) > 0)
            {
                received += count;
                if (received > MaxInstallerBytes) throw new InvalidDataException("Bộ cài vượt quá giới hạn dung lượng.");
                await output.WriteAsync(buffer.AsMemory(0, count), token).ConfigureAwait(false);
                hash.AppendData(buffer, 0, count);
                progress?.Report(total > 0 ? Math.Min(99, received * 100.0 / total.Value) : -1);
            }
            if (received == 0 || (total.HasValue && total.Value != received) ||
                !CryptographicOperations.FixedTimeEquals(hash.GetHashAndReset(), Convert.FromHexString(update.Sha256)))
                throw new InvalidDataException("Mã SHA-256 không khớp. Đã chặn cài đặt; hãy tải lại.");
        }
        EnsureNetwork();
        token.ThrowIfCancellationRequested();
        progress?.Report(100);
        return path;
    }

    public async Task<bool> DownloadAndInstallUpdateAsync(UpdateInfo updateInfo, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        string path = await DownloadVerifiedAsync(updateInfo, progress, cancellationToken).ConfigureAwait(false);
        await _install(path, updateInfo.Version, GetInstallRoot()).ConfigureAwait(false);
        return true;
    }

    internal static string GetInstallRoot() => ResolveInstallRoot(AppContext.BaseDirectory);
    internal static string ResolveInstallRoot(string appDirectory)
    {
        var dir = new DirectoryInfo(appDirectory);
        return dir.Parent?.Name.Equals("versions", StringComparison.OrdinalIgnoreCase) == true
            ? dir.Parent.Parent!.FullName : dir.FullName;
    }

    private static async Task StartInstallerAsync(string installer, string version, string installRoot)
    {
        string helper = Path.Combine(Path.GetDirectoryName(installer)!, "install-update.ps1");
        string Quote(string value) => "'" + value.Replace("'", "''") + "'";
        string script = """
            $ErrorActionPreference = 'Stop'
            try {
                $old = Get-Process -Id __PID__ -ErrorAction SilentlyContinue
                if ($old -and -not $old.WaitForExit(45000)) { throw 'App did not close; update canceled.' }
                $setupArgs = '/SILENT /SUPPRESSMSGBOXES /NORESTART /NOCLOSEAPPLICATIONS /DIR="' + __ROOT__ + '"'
                $setup = Start-Process -FilePath __SETUP__ -ArgumentList $setupArgs -PassThru -Wait
                if ($setup.ExitCode -ne 0) { throw ('Installer exit code: ' + $setup.ExitCode) }
                Start-Process -FilePath (Join-Path __ROOT__ __EXE__) -WindowStyle Hidden
            } catch {
                $_ | Out-String | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'update-error.txt') -Encoding UTF8
                Add-Type -AssemblyName PresentationFramework
                [System.Windows.MessageBox]::Show($_.Exception.Message, 'Zuzong Dynamic update') | Out-Null
            }
            """;
        script = script.Replace("__PID__", Environment.ProcessId.ToString())
            .Replace("__ROOT__", Quote(installRoot)).Replace("__SETUP__", Quote(installer))
            .Replace("__EXE__", Quote($"versions\\{version}\\ZuzongDynamic.exe"));
        await File.WriteAllTextAsync(helper, script, new System.Text.UTF8Encoding(true)).ConfigureAwait(false);
        var start = new ProcessStartInfo("powershell.exe") { UseShellExecute = false, CreateNoWindow = true };
        foreach (string arg in new[] { "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-File", helper }) start.ArgumentList.Add(arg);
        using var process = Process.Start(start) ?? throw new IOException("Không mở được trình cài cập nhật.");
        await Application.Current.Dispatcher.InvokeAsync(() => Application.Current.Shutdown());
    }

    private void EnsureNetwork()
    {
        if (!_networkAllowed()) throw new HttpRequestException("Cập nhật bị tắt trong chế độ chỉ dùng ngoại tuyến.");
    }

    private async Task<HttpResponseMessage> GetAsync(Uri uri, bool asset, CancellationToken token)
    {
        for (int redirect = 0; redirect < 6; redirect++)
        {
            EnsureNetwork();
            bool trusted = uri.Scheme == "https" && uri.IsDefaultPort && uri.UserInfo.Length == 0 &&
                (asset ? uri.Host is "github.com" or "release-assets.githubusercontent.com" or "objects.githubusercontent.com" : uri.Host == "api.github.com");
            if (!trusted) throw new InvalidDataException("Địa chỉ cập nhật không thuộc GitHub được cho phép.");
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
            if ((int)response.StatusCode is not (301 or 302 or 303 or 307 or 308)) return response;
            var location = response.Headers.Location;
            response.Dispose();
            if (location == null) throw new InvalidDataException("Chuyển hướng cập nhật không hợp lệ.");
            uri = location.IsAbsoluteUri ? location : new Uri(uri, location);
        }
        throw new InvalidDataException("Quá nhiều chuyển hướng cập nhật.");
    }

    private static async Task<byte[]> ReadLimitedAsync(HttpContent content, int limit, CancellationToken token)
    {
        await using var input = await content.ReadAsStreamAsync(token).ConfigureAwait(false);
        using var output = new MemoryStream();
        byte[] buffer = new byte[8192];
        int count;
        while ((count = await input.ReadAsync(buffer, token).ConfigureAwait(false)) > 0)
        {
            if (output.Length + count > limit) throw new InvalidDataException("Dữ liệu cập nhật quá lớn.");
            output.Write(buffer, 0, count);
        }
        return output.ToArray();
    }

    public void Dispose() => _client.Dispose();
}
