using System.Windows;
using System.Windows.Media.Imaging;

namespace VNotch.Services
{
    // Compatibility with the shared media pipeline; Lite always uses center cropping.
    public sealed class SmartThumbnailCropService : IDisposable
    {
        public bool IsLoaded => false;
        public bool TryInitialize() => false;
        public void Unload() { }
        public void Dispose() { }
        internal static void TryMinimizeNativeHeap(bool trimWorkingSet = false) { }
        public SubjectBounds? GetDominantSubjectBounds(BitmapSource source) => null;
        public Int32Rect? GetSmartCropRect(BitmapSource source, int targetSquareSize) => null;
        public Task<Int32Rect?> GetSmartCropRectAsync(BitmapSource source, int targetSquareSize, bool useObjectDetection = false, CancellationToken cancellationToken = default) => Task.FromResult<Int32Rect?>(null);
    }
    public readonly record struct SubjectBounds(float CenterX, float CenterY, float Width, float Height, float Confidence, int ClassId);
}

namespace VNotch.Services.Translation
{
    // Local inference is not shipped in Lite. No model/runtime is loaded by these adapters.
    internal sealed class QwenTranslationEngine : ILocalTranslationEngine
    {
        internal QwenTranslationEngine(TranslationModelStore store, bool preferGpu = true, Task? previousDisposal = null) { }
        internal Task Disposal => Task.CompletedTask;
        public Task<string> TranslateAsync(string text, string sourceLanguage, string targetLanguage, CancellationToken cancellationToken) => Task.FromException<string>(new NotSupportedException("Local translation is not included in Lite."));
        public void ReleaseIdleResources() { }
        public void Dispose() { }
        internal static string Glossary(string source, string target, bool enableHumanizer = true) => "";
    }
    internal static class TranslationLanguages
    {
        internal sealed record Language(string Code, string Name);
        internal static readonly string[] Codes = ["en", "vi"];
        internal static readonly Language[] All = [new("en", "English"), new("vi", "Tiếng Việt")];
        internal static bool IsSupported(string? code) => code is "en" or "vi";
        internal static string Normalize(string? code, bool source = false) => IsSupported(code) ? code! : source ? "auto" : "vi";
        internal static string ModelTarget(string code) => code;
        internal static string EnglishName(string code) => code == "vi" ? "Vietnamese" : "English";
        internal static string? Detect(string text) => null;
        internal static string? DetectForPassthrough(string text) => null;
        internal static Task<string?> DetectForPassthroughAsync(string text, CancellationToken ct = default) => Task.FromResult<string?>(null);
        internal static string? DetectUnambiguousScript(string text) => null;
    }
}
