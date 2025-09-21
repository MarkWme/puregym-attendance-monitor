using GymAttendanceMonitor.Models;

namespace GymAttendanceMonitor.Services;

public class HybridGymMonitorService : IDisposable
{
    private readonly PythonGymClient _pythonClient;
    private System.Threading.Timer? _pollingTimer;

    public HybridGymMonitorService(string pythonScriptPath)
    {
        _pythonClient = new PythonGymClient(pythonScriptPath);
    }

    public async Task<bool> InitializeAsync()
    {
        try
        {
            Console.Write("Testing Python script access... ");
            var testResult = await _pythonClient.GetAttendanceAsync();

            if (testResult != null)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓ Success");
                Console.ResetColor();
                return true;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("✗ Failed to get data from Python script");
                Console.ResetColor();
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"✗ Error: {ex.Message}");
            Console.ResetColor();
            return false;
        }
    }

    public async Task<AttendanceResponse?> CheckAttendanceAsync()
    {
        try
        {
            return await _pythonClient.GetAttendanceAsync();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error checking attendance: {ex.Message}");
            Console.ResetColor();
            return null;
        }
    }

    public void StartMonitoring(TimeSpan interval)
    {
        Console.WriteLine($"Starting monitoring with {interval.TotalMinutes} minute intervals...");
        Console.WriteLine("Press Ctrl+C to stop or Enter to check now\\n");

        _pollingTimer = new System.Threading.Timer(async _ =>
        {
            await DisplayAttendanceAsync();
        }, null, TimeSpan.Zero, interval);
    }

    public async Task DisplayAttendanceAsync()
    {
        var attendance = await CheckAttendanceAsync();

        if (attendance != null)
        {
            var level = AttendanceLevel.GetLevel(attendance.TotalPeopleInGym);
            var timestamp = DateTime.Now.ToString("HH:mm:ss");

            Console.Write($"[{timestamp}] 🏋️  Attendance: ");
            Console.ForegroundColor = level.Color;
            Console.Write($"{attendance.TotalPeopleInGym}");
            Console.ResetColor();
            Console.Write($" ({level.Description})");
            Console.WriteLine($" - Last updated: {attendance.LastRefreshed:HH:mm}");
        }
    }

    public void Dispose()
    {
        _pollingTimer?.Dispose();
        _pythonClient?.Dispose();
    }
}