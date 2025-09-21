#if WINDOWS
using GymAttendanceMonitor.Services;
using GymAttendanceMonitor.Models;
using System.Drawing;
using System.Windows.Forms;

namespace GymAttendance;

public class TrayApplication : ApplicationContext
{
    private readonly NotifyIcon _notifyIcon;
    private readonly GymMonitorService _monitor;
    private readonly System.Threading.Timer _updateTimer;
    private readonly int _intervalMinutes;

    public TrayApplication(string email, string pin, string gym, int intervalMinutes)
    {
        _intervalMinutes = intervalMinutes;
        _monitor = new GymMonitorService(email, pin, gym);

        // Create the tray icon
        _notifyIcon = new NotifyIcon()
        {
            Icon = CreateAttendanceIcon(0), // Default icon
            Text = "PureGym Attendance Monitor",
            Visible = true
        };

        // Create context menu
        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Check Now", null, OnCheckNow);
        contextMenu.Items.Add("-"); // Separator
        contextMenu.Items.Add("Settings", null, OnSettings);
        contextMenu.Items.Add("-"); // Separator
        contextMenu.Items.Add("Exit", null, OnExit);

        _notifyIcon.ContextMenuStrip = contextMenu;
        _notifyIcon.DoubleClick += OnCheckNow;

        // Start monitoring
        InitializeAsync();

        // Set up periodic updates
        _updateTimer = new System.Threading.Timer(async _ => await UpdateAttendance(),
            null, TimeSpan.Zero, TimeSpan.FromMinutes(intervalMinutes));
    }

    private async void InitializeAsync()
    {
        try
        {
            if (await _monitor.InitializeAsync())
            {
                _notifyIcon.ShowBalloonTip(3000, "PureGym Monitor", "Connected successfully!", ToolTipIcon.Info);
                await UpdateAttendance();
            }
            else
            {
                _notifyIcon.ShowBalloonTip(5000, "PureGym Monitor", "Failed to connect. Check credentials.", ToolTipIcon.Error);
            }
        }
        catch (Exception ex)
        {
            _notifyIcon.ShowBalloonTip(5000, "PureGym Monitor", $"Error: {ex.Message}", ToolTipIcon.Error);
        }
    }

    private async Task UpdateAttendance()
    {
        try
        {
            var attendance = await _monitor.CheckAttendanceAsync();
            if (attendance != null)
            {
                var level = AttendanceLevel.GetLevel(attendance.TotalPeopleInGym);

                // Update icon
                _notifyIcon.Icon = CreateAttendanceIcon(attendance.TotalPeopleInGym);

                // Update tooltip
                _notifyIcon.Text = $"🏋️ {attendance.TotalPeopleInGym} people ({level.Description})\nLast updated: {attendance.LastRefreshed:HH:mm}";
            }
        }
        catch (Exception ex)
        {
            _notifyIcon.Text = $"Error: {ex.Message}";
        }
    }

    private Icon CreateAttendanceIcon(int attendance)
    {
        // Create a simple icon with the attendance number
        var bitmap = new Bitmap(16, 16);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            // Get color based on attendance level
            var level = AttendanceLevel.GetLevel(attendance);
            var color = GetColorFromConsoleColor(level.Color);

            // Fill background
            graphics.FillEllipse(new SolidBrush(color), 0, 0, 16, 16);

            // Draw number (simplified for small icon)
            if (attendance < 100)
            {
                var text = attendance.ToString();
                var font = new Font("Arial", 6, FontStyle.Bold);
                var textSize = graphics.MeasureString(text, font);
                var x = (16 - textSize.Width) / 2;
                var y = (16 - textSize.Height) / 2;

                graphics.DrawString(text, font, Brushes.White, x, y);
            }
        }

        var iconHandle = bitmap.GetHicon();
        return Icon.FromHandle(iconHandle);
    }

    private static Color GetColorFromConsoleColor(ConsoleColor consoleColor)
    {
        return consoleColor switch
        {
            ConsoleColor.Green => Color.Green,
            ConsoleColor.Yellow => Color.Orange,
            ConsoleColor.Red => Color.Red,
            ConsoleColor.DarkRed => Color.DarkRed,
            _ => Color.Gray
        };
    }

    private async void OnCheckNow(object? sender, EventArgs e)
    {
        await UpdateAttendance();
        _notifyIcon.ShowBalloonTip(1000, "PureGym Monitor", "Attendance updated!", ToolTipIcon.Info);
    }

    private void OnSettings(object? sender, EventArgs e)
    {
        // Show settings dialog (to be implemented)
        MessageBox.Show($"Update interval: {_intervalMinutes} minutes\n\nSettings dialog coming soon!",
                       "Settings", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void OnExit(object? sender, EventArgs e)
    {
        _notifyIcon.Visible = false;
        _updateTimer?.Dispose();
        _monitor?.Dispose();
        Application.Exit();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _notifyIcon?.Dispose();
            _updateTimer?.Dispose();
            _monitor?.Dispose();
        }
        base.Dispose(disposing);
    }
}
#endif