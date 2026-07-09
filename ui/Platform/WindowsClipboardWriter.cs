using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace Clipwell.Ui.Platform;

/// <summary>
/// Writes text + HTML to the Windows clipboard in one go, mirroring the daemon
/// watcher's read path (CF_UNICODETEXT + registered "HTML Format"). The stored
/// HtmlContent is the raw CF_HTML payload (descriptor header included), so it
/// round-trips byte-for-byte and rich-text targets keep formatting on paste.
/// </summary>
[SupportedOSPlatform("windows")]
internal static class WindowsClipboardWriter
{
    private const uint CF_UNICODETEXT = 13;
    private const uint GMEM_MOVEABLE = 0x0002;

    public static bool TrySetTextAndHtml(string text, string html)
    {
        var cfHtml = RegisterClipboardFormat("HTML Format");
        if (cfHtml == 0) return false;

        // The clipboard can be briefly held by another app — retry a few times.
        var opened = false;
        for (var attempt = 0; attempt < 5 && !(opened = OpenClipboard(IntPtr.Zero)); attempt++)
            System.Threading.Thread.Sleep(10);
        if (!opened) return false;

        try
        {
            if (!EmptyClipboard()) return false;
            var textBytes = Encoding.Unicode.GetBytes(text + "\0");
            var htmlBytes = Encoding.UTF8.GetBytes(html + "\0");
            return SetData(CF_UNICODETEXT, textBytes) && SetData(cfHtml, htmlBytes);
        }
        finally
        {
            CloseClipboard();
        }
    }

    private static bool SetData(uint format, byte[] bytes)
    {
        var handle = GlobalAlloc(GMEM_MOVEABLE, (nuint)bytes.Length);
        if (handle == IntPtr.Zero) return false;
        var ptr = GlobalLock(handle);
        if (ptr == IntPtr.Zero)
        {
            GlobalFree(handle);
            return false;
        }
        Marshal.Copy(bytes, 0, ptr, bytes.Length);
        GlobalUnlock(handle);
        if (SetClipboardData(format, handle) == IntPtr.Zero)
        {
            GlobalFree(handle); // ownership only transfers on success
            return false;
        }
        return true;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool CloseClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EmptyClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint RegisterClipboardFormat(string lpszFormat);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalAlloc(uint uFlags, nuint dwBytes);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalUnlock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalFree(IntPtr hMem);
}
