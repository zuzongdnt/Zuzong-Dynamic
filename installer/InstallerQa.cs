using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.Win32;

internal static class InstallerQa
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct StartupInfo
    {
        public int cb;
        public string reserved, desktop, title;
        public int x, y, width, height, xchars, ychars, fill, flags;
        public short show, reservedSize;
        public IntPtr reservedBytes, stdin, stdout, stderr;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessInfo { public IntPtr process, thread; public int pid, threadId; }
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateDesktop(string name, IntPtr device, IntPtr mode, int flags, uint access, IntPtr security);
    [DllImport("user32.dll")] private static extern bool CloseDesktop(IntPtr desktop);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CreateProcess(string app, StringBuilder command, IntPtr processSecurity, IntPtr threadSecurity, bool inherit, int flags, IntPtr environment, string cwd, ref StartupInfo startup, out ProcessInfo info);
    [DllImport("kernel32.dll")] private static extern bool CloseHandle(IntPtr handle);
    [DllImport("kernel32.dll")] private static extern uint WaitForSingleObject(IntPtr handle, uint timeout);
    [DllImport("kernel32.dll")] private static extern bool GetExitCodeProcess(IntPtr handle, out uint exitCode);
    private sealed class Child : IDisposable
    {
        public IntPtr Handle;
        public int Id;
        public bool WaitForExit(int timeout) { return WaitForSingleObject(Handle, (uint)timeout) == 0; }
        public int ExitCode { get { uint code; if (!GetExitCodeProcess(Handle, out code)) throw new System.ComponentModel.Win32Exception(); return (int)code; } }
        public ProcessModuleCollection Modules { get { using (var view = Process.GetProcessById(Id)) return view.Modules; } }
        public void Refresh() { }
        public void Dispose() { CloseHandle(Handle); }
    }
    private static string Desktop;
    private static string Root;
    private static int Count;
    private static void Check(bool ok, string text)
    {
        if (!ok) throw new InvalidOperationException(text);
        Count++; Console.WriteLine("PASS " + text);
    }
    private static Child Start(string executable, string arguments)
    {
        var start = new StartupInfo { cb = Marshal.SizeOf(typeof(StartupInfo)), desktop = Desktop };
        ProcessInfo info;
        if (!CreateProcess(executable, new StringBuilder("\"" + executable + "\" " + arguments), IntPtr.Zero, IntPtr.Zero, false, 0x08000000, IntPtr.Zero, Path.GetDirectoryName(executable), ref start, out info))
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        CloseHandle(info.thread);
        return new Child { Handle = info.process, Id = info.pid };
    }
    private static int Run(string executable, string arguments, int timeout)
    {
        using (var process = Start(executable, arguments))
        {
            if (!process.WaitForExit(timeout)) throw new TimeoutException("Own installer process exceeded timeout: " + process.Id);
            return process.ExitCode;
        }
    }
    private static void WaitFor(Func<bool> condition, int timeout, string text)
    {
        var timer = Stopwatch.StartNew();
        while (!condition() && timer.ElapsedMilliseconds < timeout) Thread.Sleep(200);
        Check(condition(), text);
    }
    public static int Main(string[] args)
    {
        IntPtr desktop = IntPtr.Zero;
        try
        {
            string workspace = Path.GetFullPath(args[0]).TrimEnd(Path.DirectorySeparatorChar);
            Root = Path.Combine(workspace, "work", "installer-qa101");
            Check(Root.StartsWith(workspace + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase), "QA installation stays inside workspace");
            string install = Path.Combine(Root, "Installed");
            if (Directory.Exists(install))
            {
                Check(args.Contains("--resume"), "existing QA target requires explicit resume");
                using (var previous = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\ZuzongDynamic.QA_is1"))
                    Check(previous != null && string.Equals(Path.GetFullPath((string)previous.GetValue("InstallLocation")).TrimEnd('\\'), install, StringComparison.OrdinalIgnoreCase), "resumed target belongs to this isolated QA installer");
            }
            else Check(true, "QA installation target is fresh");
            Directory.CreateDirectory(Root);
            Desktop = "ZuzongInstallerQA-" + Process.GetCurrentProcess().Id;
            desktop = CreateDesktop(Desktop, IntPtr.Zero, IntPtr.Zero, 0, 0xCF, IntPtr.Zero);
            Check(desktop != IntPtr.Zero, "private desktop created without switching the user desktop");
            string setup = Path.Combine(workspace, "work", "installer-test-build101", "ZuzongDynamic-Setup-QA.exe");
            string options = "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /NOCLOSEAPPLICATIONS /NOICONS /TASKS=\"\" /LANG=vietnamese";
            Check(Run(setup, options + " /DIR=\"" + install + "\" /LOG=\"" + Path.Combine(Root, "install.log") + "\"", 180000) == 0, "setup installs successfully without launching the app");
            string bin = Path.Combine(install, "versions", "1.0.1");
            string app = Path.Combine(bin, "ZuzongDynamic.exe");
            Check(File.Exists(app) && File.Exists(Path.Combine(install, "unins000.exe")), "application and Windows uninstaller are installed");
            Check(File.Exists(Path.Combine(bin, "coreclr.dll")) && File.Exists(Path.Combine(bin, "PresentationFramework.dll")), "installed app includes CLR and WPF runtime");
            Check(!File.Exists(Path.Combine(install, "Bridge", "config.json")) && !Directory.Exists(Path.Combine(install, "NotchFull")), "no personal credentials or machine-specific notify config installed");
            const string uninstallKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\ZuzongDynamic.QA_is1";
            using (var key = Registry.CurrentUser.OpenSubKey(uninstallKey)) Check(key != null && (string)key.GetValue("DisplayName") == "Zuzong Dynamic QA", "per-user Apps and Features entry registered");
            using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run")) Check(key == null || key.GetValue("ZuzongDynamic.QA") == null, "unchecked autostart is not enabled");
            string profile = Path.Combine(Root, "Profile"); Directory.CreateDirectory(profile);
            string config = Path.Combine(profile, "notch.json");
            string enabled = "{\"enabled\":true,\"media\":false,\"glass\":false,\"files\":false,\"battery\":false,\"width\":230,\"expandedWidth\":480,\"style\":\"attached\",\"top\":0,\"followTaskbar\":false,\"hideFullscreen\":false,\"clipboardNotifications\":false,\"codexNotifications\":false}";
            File.WriteAllText(config, enabled);
            File.WriteAllText(Path.Combine(profile, "keep-my-settings.txt"), "installer QA sentinel");
            using (var process = Start(app, "--settings \"" + config + "\" --isolated-data --background"))
            {
                string runtime = Path.Combine(profile, "notch-runtime.json");
                WaitFor(() => File.Exists(runtime) && File.ReadAllText(runtime).Contains("\"pid\":" + process.Id), 20000, "installed standalone executable starts on its isolated profile");
                process.Refresh();
                bool localClr = false;
                foreach (ProcessModule module in process.Modules)
                    if (string.Equals(module.ModuleName, "coreclr.dll", StringComparison.OrdinalIgnoreCase))
                        localClr = string.Equals(Path.GetFullPath(module.FileName), Path.Combine(bin, "coreclr.dll"), StringComparison.OrdinalIgnoreCase);
                Check(localClr, "running installed app loads its bundled CLR, not the machine runtime");
                int blocked = Run(Path.Combine(install, "unins000.exe"), options + " /LOG=\"" + Path.Combine(Root, "blocked-uninstall.log") + "\"", 20000);
                Check(blocked != 0 && File.Exists(app), "uninstall refuses while this app is running");
                File.WriteAllText(config, enabled.Replace("\"enabled\":true", "\"enabled\":false"));
                Check(process.WaitForExit(15000), "QA app closes gracefully through its own configuration");
            }
            Check(Run(Path.Combine(install, "unins000.exe"), options + " /LOG=\"" + Path.Combine(Root, "uninstall.log") + "\"", 120000) == 0, "uninstall completes after the app exits");
            Check(!File.Exists(app) && !File.Exists(Path.Combine(bin, "coreclr.dll")), "uninstall removes the installed app and bundled runtime");
            using (var key = Registry.CurrentUser.OpenSubKey(uninstallKey)) Check(key == null, "uninstall removes its own Apps and Features entry");
            Check(File.ReadAllText(Path.Combine(profile, "keep-my-settings.txt")) == "installer QA sentinel" && File.Exists(config), "uninstall preserves the separate user profile");
            Console.WriteLine("PASS TOTAL " + Count); return 0;
        }
        catch (Exception ex) { Console.WriteLine("FAIL " + ex); return 1; }
        finally { if (desktop != IntPtr.Zero) CloseDesktop(desktop); }
    }
}
