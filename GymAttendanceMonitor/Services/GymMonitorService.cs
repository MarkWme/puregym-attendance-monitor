using GymAttendanceMonitor.Models;

namespace GymAttendanceMonitor.Services;

public class GymMonitorService : IDisposable
{
    private readonly PureGymApiClient _apiClient;
    private readonly string _gymName;
    private readonly bool _debug;
    private int _currentGymId;
    private System.Threading.Timer? _pollingTimer;

    public GymMonitorService(string email, string pin, string gymName, bool debug = false)
    {
        _apiClient = new PureGymApiClient(email, pin, debug);
        _gymName = gymName;
        _debug = debug;
    }

    public async Task<bool> InitializeAsync()
    {
        try
        {
            Console.Write("Authenticating... ");
            if (await _apiClient.AuthenticateAsync())
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓ Success");
                Console.ResetColor();

                if (_gymName.ToLowerInvariant() == "home")
                {
                    Console.Write("Getting your home gym... ");
                    var homeGym = await _apiClient.GetHomeGymAsync();

                    if (homeGym != null)
                    {
                        _currentGymId = homeGym.Id;
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"✓ Found: {homeGym.Name} (ID: {homeGym.Id})");
                        Console.ResetColor();
                        return true;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("✗ Could not get home gym");
                        Console.ResetColor();
                        return false;
                    }
                }
                else
                {
                    Console.Write($"Finding gym '{_gymName}'... ");
                    var gym = await _apiClient.FindGymByNameAsync(_gymName);

                    if (gym != null)
                    {
                        _currentGymId = gym.Id;
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"✓ Found: {gym.Name} (ID: {gym.Id})");
                        Console.ResetColor();
                        return true;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"✗ Gym '{_gymName}' not found");
                        Console.ResetColor();
                        return false;
                    }
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("✗ Authentication failed");
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
            return await _apiClient.GetAttendanceAsync(_currentGymId);
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
        Console.WriteLine("Press Ctrl+C to stop or Enter to check now\n");

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
        _apiClient?.Dispose();
    }
}