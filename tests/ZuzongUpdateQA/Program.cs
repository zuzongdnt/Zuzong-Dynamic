using System.Net;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using VNotch.Services;
using VNotch.Models;

static class Program
{
    static int checks;
    static void Check(bool ok, string name) { if (!ok) throw new Exception(name); Console.WriteLine("PASS " + name); checks++; }
    static string Current => AppVersion.Current;
    static string Next => (new Version(Current.Split('-')[0]).Major + 1) + ".0.0";
    static string Beta => (new Version(Current.Split('-')[0]).Major + 2) + ".0.0-beta.1";
    static readonly byte[] Installer = Encoding.UTF8.GetBytes("verified installer fixture");
    static string Hash => Convert.ToHexString(SHA256.HashData(Installer)).ToLowerInvariant();
    static object Release(string version, bool prerelease = false, bool draft = false, string? hash = null, string? url = null) => new
    {
        tag_name = "v" + version, draft, prerelease, body = "release notes", published_at = "2026-11-02T00:00:00Z",
        assets = new[] { new { name = $"ZuzongDynamic-Setup-{version}-x64.exe", digest = "sha256:" + (hash ?? Hash),
            browser_download_url = url ?? $"{ZuzongUpdateService.RepositoryUrl}/releases/download/v{version}/ZuzongDynamic-Setup-{version}-x64.exe" } }
    };
    static async Task MustFail(Func<Task> action, string name)
    {
        bool failed = false;
        try { await action(); } catch (Exception ex) when (ex is InvalidDataException or HttpRequestException or OperationCanceledException) { failed = true; }
        Check(failed, name);
    }
    static async Task Main(string[] args)
    {
        if (args.Contains("--live"))
        {
            NetworkPrivacy.Current.Apply(new NotchSettings { EnableLocalOnlyMode = false });
            using var live = new ZuzongUpdateService();
            var latest = await live.CheckForUpdatesAsync(forceRefresh: true);
            Check(latest?.Version == AppVersion.Current && !latest.IsNewerVersion, "public GitHub feed matches installed version");
            Check(latest?.Sha256.Length == 64, "GitHub provides server SHA256 digest");
            Console.WriteLine("LIVE " + latest!.DownloadUrl);
            return;
        }
        var feed = JsonSerializer.SerializeToUtf8Bytes(new[] { Release(Next), Release(Beta, true), Release("999.0.0", draft: true), Release("998.0.0", url: "https://github.com/w0otdev/V-Notch/evil.exe"), Release("997.0.0", hash: "bad") });
        var parsed = ZuzongUpdateService.ParseReleases(feed, Current);
        Check(System.Text.RegularExpressions.Regex.IsMatch(Current, @"^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?$"), "assembly version is valid Zuzong release");
        Check(parsed.Count == 2 && parsed[0].Version == Beta, "sort versions and reject drafts, foreign origin, missing digest");
        Check(ZuzongUpdateService.ParseReleases(JsonSerializer.SerializeToUtf8Bytes(new[] { Release("0.0.1") }), Current).Single().IsNewerVersion == false, "older release cannot downgrade");
        Check(ZuzongUpdateService.ResolveInstallRoot(@"E:\ZuzongDynamic\versions\1.0.1\") == @"E:\ZuzongDynamic", "resolve versioned installation root");
        Check(ZuzongUpdateService.ResolveInstallRoot(@"C:\Users\Test User\Portable\") == @"C:\Users\Test User\Portable\", "portable install path with spaces");
        var config = new NotchSettings { AutoCheckUpdates = true };
        ZuzongIntegration.MapCustomSettings(config);
        Check(config.AutoCheckUpdates, "custom mapping preserves update switch");
        bool allowed = true, launched = false;
        var handler = new Handler(feed);
        using var service = new ZuzongUpdateService(new HttpClient(handler), () => allowed, (path, version, root) => { launched = true; Check(File.ReadAllBytes(path).SequenceEqual(Installer), "only verified bytes reach installer"); return Task.CompletedTask; });
        int events = 0; service.UpdateCheckCompleted += (_, _) => events++;
        var stable = await service.CheckForUpdatesAsync();
        Check(stable?.Version == Next, "stable channel excludes beta");
        Check((await service.CheckForUpdatesAsync(true))?.Version == Beta, "beta channel includes prerelease");
        Check(handler.Requests == 1 && events == 2, "cache checks, notify both views");
        await service.CheckForUpdatesAsync(forceRefresh: true);
        Check(handler.Requests == 2, "manual check bypasses cache");
        Check(await service.DownloadAndInstallUpdateAsync(stable!), "download and install verified update");
        Check(launched, "installer callback called after verification");
        launched = false; handler.Corrupt = true;
        await MustFail(() => service.DownloadAndInstallUpdateAsync(stable!), "corrupt installer blocked by SHA256");
        Check(!launched, "corrupt installer never executes");
        handler.Corrupt = false; handler.Redirect = "http://github.com/unsafe.exe";
        await MustFail(() => service.DownloadAndInstallUpdateAsync(stable!), "HTTP redirect blocked");
        handler.Redirect = "https://evil.example/unsafe.exe";
        await MustFail(() => service.DownloadAndInstallUpdateAsync(stable!), "foreign redirect blocked");
        handler.Redirect = null;
        await MustFail(() => service.DownloadAndInstallUpdateAsync(new UpdateInfo { Version = Next, DownloadUrl = "https://evil.example/unsafe.exe" }), "caller cannot substitute an installer URL");
        allowed = false;
        await MustFail(() => service.CheckForUpdatesAsync(), "offline blocks even cached network check");
        allowed = true; handler.FeedStatus = HttpStatusCode.TooManyRequests;
        await MustFail(() => service.CheckForUpdatesAsync(forceRefresh: true), "rate limit reports error, never up-to-date");
        handler.FeedStatus = HttpStatusCode.OK;
        using var cancel = new CancellationTokenSource(); cancel.Cancel();
        await MustFail(() => service.DownloadAndInstallUpdateAsync(stable!, cancellationToken: cancel.Token), "cancel never launches installer");
        Console.WriteLine($"TOTAL {checks} PASS");
    }
    sealed class Handler(byte[] feed) : HttpMessageHandler
    {
        internal int Requests;
        internal bool Corrupt;
        internal string? Redirect;
        internal HttpStatusCode FeedStatus = HttpStatusCode.OK;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested(); Requests++;
            if (request.RequestUri!.Host == "api.github.com") return Task.FromResult(new HttpResponseMessage(FeedStatus) { Content = new ByteArrayContent(feed) });
            if (Redirect != null) { var redirect = new HttpResponseMessage(HttpStatusCode.Redirect); redirect.Headers.Location = new Uri(Redirect); return Task.FromResult(redirect); }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(Corrupt ? [1, 2, 3] : Installer) });
        }
    }
}
