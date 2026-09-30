Add-Type @'
using System;
using System.Runtime.InteropServices;
using System.Text;

public class WinHelper {
    public delegate bool EnumThreadDelegate(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern bool EnumThreadWindows(int dwThreadId, EnumThreadDelegate lpfn, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);
}
'@

$proc = Get-Process -Id 576
foreach ($thread in $proc.Threads) {
    [WinHelper]::EnumThreadWindows($thread.Id, {
        param($hwnd, $lparam)
        $sb = New-Object System.Text.StringBuilder 256
        [WinHelper]::GetWindowText($hwnd, $sb, 256) | Out-Null
        $title = $sb.ToString()
        if ($title) {
            Write-Host "HWND: $hwnd Title: $title"
            [WinHelper]::SetForegroundWindow($hwnd) | Out-Null
        }
        return $true
    }, [IntPtr]::Zero) | Out-Null
}
