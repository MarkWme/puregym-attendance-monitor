using GymAttendanceMonitor.Services;
using GymAttendanceMonitor.Models;
using System.Drawing;
using System.Windows.Forms;

namespace GymAttendanceTray;

public class TrayApplication : ApplicationContext
{
    private readonly NotifyIcon _notifyIcon;
    private readonly GymMonitorService _monitor;
    private readonly System.Threading.Timer _updateTimer;
    private readonly int _intervalMinutes;
    private readonly bool _debug;
    private AttendanceResponse? _lastAttendance;

    public TrayApplication(string email, string pin, string gym, int intervalMinutes, bool debug = false)
    {
        _intervalMinutes = intervalMinutes;
        _debug = debug;
        _monitor = new GymMonitorService(email, pin, gym, debug);

        // Create the tray icon
        _notifyIcon = new NotifyIcon()
        {
            Icon = CreateDefaultIcon(),
            Text = "PureGym Attendance Monitor - Initializing...",
            Visible = true
        };

        // Create context menu
        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Check Now", null, OnCheckNow);
        contextMenu.Items.Add("Show Details", null, OnShowDetails);
        contextMenu.Items.Add("-"); // Separator
        contextMenu.Items.Add($"Update Interval: {intervalMinutes} min", null, OnSettings).Enabled = false;
        contextMenu.Items.Add("-"); // Separator
        contextMenu.Items.Add("About", null, OnAbout);
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
            _notifyIcon.Text = "PureGym Monitor - Connecting...";

            if (await _monitor.InitializeAsync())
            {
                _notifyIcon.ShowBalloonTip(3000, "PureGym Monitor", "Connected successfully!", ToolTipIcon.Info);
                await UpdateAttendance();
            }
            else
            {
                _notifyIcon.ShowBalloonTip(5000, "PureGym Monitor", "Failed to connect. Check credentials.", ToolTipIcon.Error);
                _notifyIcon.Text = "PureGym Monitor - Connection Failed";
                _notifyIcon.Icon = CreateErrorIcon();
            }
        }
        catch (Exception ex)
        {
            var message = _debug ? ex.Message : "Connection error occurred";
            _notifyIcon.ShowBalloonTip(5000, "PureGym Monitor", message, ToolTipIcon.Error);
            _notifyIcon.Text = "PureGym Monitor - Error";
            _notifyIcon.Icon = CreateErrorIcon();
        }
    }

    private async Task UpdateAttendance()
    {
        try
        {
            var attendance = await _monitor.CheckAttendanceAsync();
            if (attendance != null)
            {
                _lastAttendance = attendance;
                var level = AttendanceLevel.GetLevel(attendance.TotalPeopleInGym);

                // Update icon with attendance number and color
                _notifyIcon.Icon = CreateAttendanceIcon(attendance.TotalPeopleInGym, level.Color);

                // Update tooltip with detailed info
                _notifyIcon.Text = $"🏋️ {attendance.TotalPeopleInGym} people ({level.Description})\n" +
                                 $"Canterbury Wincheap\n" +
                                 $"Last updated: {attendance.LastRefreshed:HH:mm}";
            }
            else
            {
                _notifyIcon.Text = "PureGym Monitor - Update Failed";
                _notifyIcon.Icon = CreateErrorIcon();
            }
        }
        catch (Exception ex)
        {
            var message = _debug ? ex.Message : "Update failed";
            _notifyIcon.Text = $"PureGym Monitor - Error: {message}";
            _notifyIcon.Icon = CreateErrorIcon();
        }
    }

    private Icon CreateAttendanceIcon(int attendance, ConsoleColor level)
    {
        var bitmap = new Bitmap(32, 32);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            // Get color based on attendance level
            var color = GetColorFromConsoleColor(level);
            var brush = new SolidBrush(color);

            // Fill background circle
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            graphics.FillEllipse(brush, 2, 2, 28, 28);

            // Draw border
            graphics.DrawEllipse(new Pen(Color.White, 2), 2, 2, 28, 28);

            // Draw attendance number
            var text = attendance > 99 ? "99+" : attendance.ToString();
            var font = new Font("Arial", attendance > 99 ? 8 : 10, FontStyle.Bold);
            var textBrush = new SolidBrush(Color.White);

            var textSize = graphics.MeasureString(text, font);
            var x = (32 - textSize.Width) / 2;
            var y = (32 - textSize.Height) / 2;

            // Draw text shadow for better visibility
            graphics.DrawString(text, font, new SolidBrush(Color.Black), x + 1, y + 1);
            graphics.DrawString(text, font, textBrush, x, y);
        }

        var iconHandle = bitmap.GetHicon();
        return Icon.FromHandle(iconHandle);
    }

    private Icon CreateDefaultIcon()
    {
        var bitmap = new Bitmap(32, 32);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            graphics.FillEllipse(new SolidBrush(Color.DarkBlue), 2, 2, 28, 28);
            graphics.DrawEllipse(new Pen(Color.White, 2), 2, 2, 28, 28);

            var font = new Font("Arial", 10, FontStyle.Bold);
            var text = "🏋️";
            var textSize = graphics.MeasureString(text, font);
            var x = (32 - textSize.Width) / 2;
            var y = (32 - textSize.Height) / 2;

            graphics.DrawString(text, font, new SolidBrush(Color.White), x, y);
        }

        var iconHandle = bitmap.GetHicon();
        return Icon.FromHandle(iconHandle);
    }

    private Icon CreateErrorIcon()
    {
        var bitmap = new Bitmap(32, 32);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            graphics.FillEllipse(new SolidBrush(Color.Red), 2, 2, 28, 28);
            graphics.DrawEllipse(new Pen(Color.White, 2), 2, 2, 28, 28);

            var font = new Font("Arial", 14, FontStyle.Bold);
            var text = "!";
            var textSize = graphics.MeasureString(text, font);
            var x = (32 - textSize.Width) / 2;
            var y = (32 - textSize.Height) / 2;

            graphics.DrawString(text, font, new SolidBrush(Color.White), x, y);
        }

        var iconHandle = bitmap.GetHicon();
        return Icon.FromHandle(iconHandle);
    }

    private static Color GetColorFromConsoleColor(ConsoleColor consoleColor)
    {
        return consoleColor switch
        {
            ConsoleColor.Green => Color.FromArgb(40, 167, 69),      // Success green
            ConsoleColor.Yellow => Color.FromArgb(255, 193, 7),     // Warning yellow
            ConsoleColor.Red => Color.FromArgb(220, 53, 69),        // Danger red
            ConsoleColor.DarkRed => Color.FromArgb(155, 20, 30),    // Dark red
            _ => Color.Gray
        };
    }

    private async void OnCheckNow(object? sender, EventArgs e)
    {
        _notifyIcon.ShowBalloonTip(1000, "PureGym Monitor", "Checking attendance...", ToolTipIcon.Info);
        await UpdateAttendance();
    }

    private void OnShowDetails(object? sender, EventArgs e)
    {
        if (_lastAttendance != null)
        {
            var level = AttendanceLevel.GetLevel(_lastAttendance.TotalPeopleInGym);
            var message = $"Current Attendance: {_lastAttendance.TotalPeopleInGym} people\n" +
                         $"Status: {level.Description}\n" +
                         $"Gym: Canterbury Wincheap\n" +
                         $"Last Updated: {_lastAttendance.LastRefreshed:HH:mm:ss}\n" +
                         $"Update Interval: {_intervalMinutes} minutes";

            MessageBox.Show(message, "PureGym Attendance Details",
                          MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else
        {
            MessageBox.Show("No attendance data available.", "PureGym Monitor",
                          MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void OnSettings(object? sender, EventArgs e)
    {
        // Settings dialog placeholder
        MessageBox.Show($"Update interval: {_intervalMinutes} minutes\n\nSettings dialog coming in future version!",
                       "Settings", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void OnAbout(object? sender, EventArgs e)
    {
        var message = "PureGym Attendance Monitor\n" +
                     "Windows Tray Application\n\n" +
                     "Monitors real-time gym attendance\n" +
                     "for Canterbury Wincheap PureGym\n\n" +
                     "Built with .NET 9 and Windows Forms";

        MessageBox.Show(message, "About PureGym Monitor",
                       MessageBoxButtons.OK, MessageBoxIcon.Information);
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