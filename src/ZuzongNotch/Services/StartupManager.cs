using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;

namespace VNotch.Services;

public static partial class StartupManager
{
    [LibraryImport("VNotch.Native.dll")]
    private static partial int vn_startup_is_enabled(out byte outEnabled);

    [LibraryImport("VNotch.Native.dll")]
    private static partial int vn_startup_set_enabled(
        byte enable, ReadOnlySpan<ushort> exePath, int exePathLen, out int outWin32Error);

    const string ZuzongKey="Software\\Microsoft\\Windows\\CurrentVersion\\Run",ZuzongName="ZuzongDynamic";
    public static bool IsAutoStartEnabled(){using var key=Microsoft.Win32.Registry.CurrentUser.OpenSubKey(ZuzongKey);return key?.GetValue(ZuzongName)!=null;}
    public static void SetAutoStart(bool enable){using var key=Microsoft.Win32.Registry.CurrentUser.CreateSubKey(ZuzongKey);if(enable)key.SetValue(ZuzongName,"\""+ZuzongIntegration.EditorPath+"\" --startup");else key.DeleteValue(ZuzongName,false);}

    internal static void RepairExistingAutoStart()
    {
        if (!ZuzongIntegration.IsIsolated && IsAutoStartEnabled()) SetAutoStart(true);
    }

    internal static void EnsureChangeSucceeded(int status, int win32Error)
    {
        if (win32Error != 0) throw new Win32Exception(win32Error);
        if (status != 0) throw new InvalidOperationException($"Startup settings failed (native status {status}).");
    }
}

public sealed class WindowsStartupManager : IStartupManager
{
    public bool IsAutoStartEnabled() => StartupManager.IsAutoStartEnabled();
    public void SetAutoStart(bool enable) => StartupManager.SetAutoStart(enable);
}
