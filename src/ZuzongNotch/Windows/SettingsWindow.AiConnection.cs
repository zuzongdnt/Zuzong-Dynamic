using System.Windows;
using System.Windows.Controls;
using VNotch.Services;
using VNotch.Services.Spotlight;

namespace VNotch;

public partial class SettingsWindow
{
    private async void SpotlightAiTest_Click(object sender, RoutedEventArgs e)
    {
        SpotlightAiTestButton.IsEnabled = false;
        SpotlightAiConnectionStatus.Text = "Đang kiểm tra API…";
        try
        {
            var snapshot = ReadSettingsFromUi();
            NetworkPrivacy.Current.Apply(snapshot);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(25));
            var service = new SpotlightAiService();
            if (snapshot.SpotlightAiProvider == "Gemini")
            {
                var models = await service.ListGeminiModelsAsync(snapshot, timeout.Token);
                SpotlightAiAvailableModels.ItemsSource = models;
                SpotlightAiAvailableModels.Visibility = Visibility.Visible;
                string current = SpotlightAiService.NormalizeModel("Gemini", SpotlightAiModelBox.Text);
                if (!models.Contains(current))
                    current = models.FirstOrDefault(m => m == "gemini-flash-lite-latest") ?? models.FirstOrDefault(m => m.Contains("flash")) ?? models.First();
                SpotlightAiModelBox.Text = current;
                SpotlightAiAvailableModels.SelectedItem = current;
                snapshot = ReadSettingsFromUi();
            }
            await service.SendAsync(snapshot, new[] { new SpotlightAiMessage("user", "Reply with OK.") }, timeout.Token);
            SpotlightAiConnectionStatus.Text = "Kết nối và nhận trả lời thành công. Bấm Lưu để giữ cấu hình.";
        }
        catch (SpotlightAiException ex) { SpotlightAiConnectionStatus.Text = Loc.Get(ex.Message); }
        catch (OperationCanceledException) { SpotlightAiConnectionStatus.Text = "Kết nối quá thời gian. Kiểm tra mạng rồi thử lại."; }
        catch (Exception) { SpotlightAiConnectionStatus.Text = "Chưa kết nối được. Kiểm tra API key, model và quyền cho phép AI trực tuyến."; }
        finally { SpotlightAiTestButton.IsEnabled = true; }
    }

    private void SpotlightAiAvailableModel_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (SpotlightAiAvailableModels.SelectedItem is string model) SpotlightAiModelBox.Text = model;
    }
}
