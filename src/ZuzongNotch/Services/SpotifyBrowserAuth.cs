using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace VNotch.Services;

internal static class SpotifyBrowserAuth
{
    internal const string RedirectUri = "http://127.0.0.1:43821/callback";
    static readonly HttpClient Client = new(NetworkPrivacy.Handler(NetworkFeature.Canvas)) { Timeout = TimeSpan.FromSeconds(25) };
    static string TokenPath => Path.Combine(AppPaths.LocalAppDataRoot, "spotify-browser-session.json");
    static string OptionsPath => Path.Combine(AppPaths.LocalAppDataRoot, "spotify-browser-options.json");
    internal static bool HasSession => File.Exists(TokenPath);
    internal static string ReadClientId()
    {
        try { return JsonSerializer.Deserialize<string>(File.ReadAllText(OptionsPath)) ?? ""; }
        catch { return ""; }
    }
    internal static string Challenge(string verifier) => Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
    static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    internal static bool IsClientId(string value) => value.Length == 32 && value.All(Uri.IsHexDigit);

    internal static Uri AuthorizationUri(string clientId, string state, string challenge) => new(
        "https://accounts.spotify.com/authorize?response_type=code&client_id=" + Uri.EscapeDataString(clientId) +
        "&redirect_uri=" + Uri.EscapeDataString(RedirectUri) + "&code_challenge_method=S256&code_challenge=" + Uri.EscapeDataString(challenge) +
        "&state=" + Uri.EscapeDataString(state) + "&scope=" + Uri.EscapeDataString("user-read-private user-read-currently-playing user-read-playback-state"));

    internal static Dictionary<string, string> ParseCallback(string target) => new Uri("http://127.0.0.1" + target).Query.TrimStart('?')
        .Split('&', StringSplitOptions.RemoveEmptyEntries).Select(part => part.Split('=', 2))
        .GroupBy(p => Uri.UnescapeDataString(p[0])).ToDictionary(g => g.Key, g => g.Count() == 1 ? Uri.UnescapeDataString(g.First().ElementAtOrDefault(1) ?? "") : "");

    internal static async Task ConnectAsync(string clientId, CancellationToken token)
    {
        if (!IsClientId(clientId)) throw new InvalidOperationException("Client ID cần đủ 32 ký tự hexadecimal trong Spotify Developer Dashboard.");
        using var privacy = CancellationTokenSource.CreateLinkedTokenSource(token, NetworkPrivacy.Current.Acquire(NetworkFeature.Canvas));
        token = privacy.Token;
        string verifier = Base64Url(RandomNumberGenerator.GetBytes(64));
        string state = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var listener = new TcpListener(IPAddress.Loopback, 43821);
        listener.Start();
        string? code = null;
        try
        {
            if (!SafeLauncher.TryOpenUrl(AuthorizationUri(clientId, state, Challenge(verifier)).AbsoluteUri))
                throw new InvalidOperationException("Không mở được trình duyệt. Kiểm tra quyền mở liên kết ngoài.");
            while (code == null)
            {
                using var socket = await listener.AcceptTcpClientAsync(token);
                using var stream = socket.GetStream();
                using var readTimeout = CancellationTokenSource.CreateLinkedTokenSource(token); readTimeout.CancelAfter(TimeSpan.FromSeconds(5));
                using var reader = new StreamReader(stream, Encoding.ASCII, false, 4096, true);
                string? line = await reader.ReadLineAsync(readTimeout.Token);
                if (line == null || line.Length > 8192) continue;
                string[] parts = line.Split(' ');
                if (parts.Length != 3 || parts[0] != "GET" || !parts[1].StartsWith("/callback?", StringComparison.Ordinal)) continue;
                var query = ParseCallback(parts[1]);
                bool matches = query.TryGetValue("state", out var returned) && returned.Length == state.Length && CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(state), Encoding.ASCII.GetBytes(returned));
                string text = matches ? "Zuzong Dynamic da nhan phan hoi. Ban co the quay lai ung dung." : "Phan hoi khong hop le. Hay mo lai ket noi tu Zuzong Dynamic.";
                byte[] body = Encoding.UTF8.GetBytes(text);
                await stream.WriteAsync(Encoding.ASCII.GetBytes($"HTTP/1.1 {(matches ? "200 OK" : "400 Bad Request")}\r\nContent-Type: text/plain; charset=utf-8\r\nContent-Length: {body.Length}\r\nConnection: close\r\nCache-Control: no-store\r\n\r\n"), token);
                await stream.WriteAsync(body, token);
                if (!matches) continue;
                if (query.ContainsKey("error")) throw new InvalidOperationException("Bạn đã hủy cấp quyền Spotify. Có thể kết nối lại bất cứ lúc nào.");
                if (!query.TryGetValue("code", out code) || string.IsNullOrWhiteSpace(code)) throw new InvalidOperationException("Spotify chưa trả mã đăng nhập.");
            }
        }
        finally { listener.Stop(); }
        using var response = await Client.PostAsync("https://accounts.spotify.com/api/token", new FormUrlEncodedContent(new Dictionary<string, string>
        { ["grant_type"] = "authorization_code", ["code"] = code, ["redirect_uri"] = RedirectUri, ["client_id"] = clientId, ["code_verifier"] = verifier }), token);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"Spotify chưa cấp phiên đăng nhập (HTTP {(int)response.StatusCode}). Kiểm tra Client ID và Redirect URI.");
        string payload = await response.Content.ReadAsStringAsync(token);
        using var json = JsonDocument.Parse(payload);
        if (!json.RootElement.TryGetProperty("access_token", out var access) || string.IsNullOrWhiteSpace(access.GetString())) throw new InvalidOperationException("Phiên Spotify chưa đầy đủ.");
        Directory.CreateDirectory(AppPaths.LocalAppDataRoot);
        string temp = TokenPath + ".new";
        File.WriteAllText(temp, DataProtection.Protect(payload)); File.Move(temp, TokenPath, true);
        File.WriteAllText(OptionsPath, JsonSerializer.Serialize(clientId));
    }
}
