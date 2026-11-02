using System.Windows;
using VNotch.Services;

namespace VNotch;

public partial class SettingsWindow
{
    internal const string ZuzongGithubUrl = "https://github.com/zuzongdnt";
    private void CopySupportAccount_Click(object sender, RoutedEventArgs e)
    {
        try { Clipboard.SetText("1050374824"); }
        catch { MessageBox.Show(this, "Clipboard Ä‘ang báº­n. Zuzong thá»­ láº¡i sau nhÃ©.", "Zuzong Dynamic"); }
    }
    private void ZuzongGithub_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(ZuzongGithubUrl)) SafeLauncher.TryOpenUrl(ZuzongGithubUrl);
    }
    private void ZuzongTiktok_Click(object sender, RoutedEventArgs e) => SafeLauncher.TryOpenUrl("https://www.tiktok.com/@_thinhdzuong_");
}
