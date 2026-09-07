using System.Windows;
using System.Windows.Interop;
using Forms = System.Windows.Forms;
using StatsScreen.Services.Logging;

namespace StatsScreen.Services.Display;

public sealed class DisplayService
{
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpShowWindow = 0x0040;
    private static readonly IntPtr HwndTopmost = new(-1);
    private static readonly IntPtr HwndNotTopmost = new(-2);

    private readonly IAppLogger _logger;

    public DisplayService(IAppLogger logger)
    {
        _logger = logger;
    }

    public IReadOnlyList<DisplayInfo> GetDisplays()
    {
        try
        {
            return Forms.Screen.AllScreens
                .Select((screen, index) => new DisplayInfo(
                    screen.DeviceName,
                    $"Display {index + 1}",
                    screen.Bounds.X,
                    screen.Bounds.Y,
                    screen.Bounds.Width,
                    screen.Bounds.Height,
                    screen.Primary))
                .OrderByDescending(display => display.IsPrimary)
                .ThenBy(display => display.DeviceName, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch (Exception exception)
        {
            _logger.Error("Unable to enumerate Windows displays.", exception);
            return Array.Empty<DisplayInfo>();
        }
    }

    public DisplayInfo SelectDisplay(string? savedDeviceName)
    {
        IReadOnlyList<DisplayInfo> displays = GetDisplays();
        return displays.FirstOrDefault(display =>
                   string.Equals(display.DeviceName, savedDeviceName, StringComparison.OrdinalIgnoreCase))
               ?? displays.FirstOrDefault(display => display.IsPrimary)
               ?? displays.FirstOrDefault()
               ?? new DisplayInfo("", "No display", 0, 0, 800, 600, true);
    }

    public void EnterDisplayMode(Window window, DisplayInfo display)
    {
        WindowInteropHelper helper = new(window);
        IntPtr handle = helper.Handle;
        if (handle == IntPtr.Zero)
        {
            _logger.Warning("Display mode requested before the window had a native handle.");
            return;
        }

        window.WindowState = WindowState.Normal;
        window.WindowStyle = WindowStyle.None;
        window.ResizeMode = ResizeMode.NoResize;
        window.ShowInTaskbar = false;
        window.Topmost = true;

        SetWindowPos(
            handle,
            HwndTopmost,
            display.X,
            display.Y,
            display.Width,
            display.Height,
            SwpNoActivate | SwpShowWindow);
    }

    public void MoveDisplayMode(Window window, DisplayInfo display)
    {
        WindowInteropHelper helper = new(window);
        IntPtr handle = helper.Handle;
        if (handle == IntPtr.Zero)
        {
            _logger.Warning("Display mode move requested before the window had a native handle.");
            return;
        }

        SetWindowPos(
            handle,
            HwndTopmost,
            display.X,
            display.Y,
            display.Width,
            display.Height,
            SwpNoActivate | SwpShowWindow);
    }

    public void ExitDisplayMode(Window window)
    {
        WindowInteropHelper helper = new(window);
        IntPtr handle = helper.Handle;
        window.Topmost = false;
        window.ShowInTaskbar = true;
        window.WindowStyle = WindowStyle.SingleBorderWindow;
        window.ResizeMode = ResizeMode.CanResize;

        if (handle != IntPtr.Zero)
        {
            SetWindowPos(handle, HwndNotTopmost, 0, 0, 0, 0, SwpNoActivate | SwpNoMove | SwpNoSize);
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint uFlags);
}
