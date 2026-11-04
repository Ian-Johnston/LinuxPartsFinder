using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;

namespace LinuxPartsFinder;

/// <summary>
/// Dark grey's title bar: the window's own slate (#2C3038) rather than the system's black/neutral dark one
/// (as WinPartsFinder's Theme.vb). Every other theme keeps the normal title bar. App applies it to each window.
/// Windows only (for trying the app out on a PC): on Linux the desktop draws the title bar, so it keeps its own colours.
/// </summary>
public static class TitleBar
{
    private const byte SlateR = 0x2C, SlateG = 0x30, SlateB = 0x38;

    /// <summary>Colours this window's title bar for the current theme.</summary>
    public static void Apply(Window window)
    {
        var slate = Application.Current?.ActualThemeVariant == AppThemes.DarkGrey;
        var handle = window.TryGetPlatformHandle();
        if (handle == null || handle.Handle == IntPtr.Zero) return;
        try
        {
            if (OperatingSystem.IsWindows()) ApplyWindows(handle.Handle, slate);
        }
        catch
        {
            // older system: the title bar just keeps its normal colour
        }
    }

    // ---- Windows: the caption colour (Windows 11 only - Windows 10 ignores it and keeps the black one) ----
    private const int DWMWA_CAPTION_COLOR = 35;
    private const int DWMWA_COLOR_DEFAULT = unchecked((int)0xFFFFFFFF);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    private static void ApplyWindows(IntPtr hwnd, bool slate)
    {
        var colour = slate ? (SlateB << 16) | (SlateG << 8) | SlateR : DWMWA_COLOR_DEFAULT; // COLORREF is 0x00BBGGRR
        DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, ref colour, 4);
    }
}
