using System.Diagnostics;
using System.Text.RegularExpressions;
using GymAttendanceMonitor.Models;

namespace GymAttendanceMonitor.Services;

public class PythonGymClient : IDisposable
{
    private readonly string _pythonScriptPath;
    private bool _disposed = false;

    public PythonGymClient(string pythonScriptPath)
    {
        _pythonScriptPath = pythonScriptPath;
    }

    public async Task<AttendanceResponse?> GetAttendanceAsync()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = $"\"{_pythonScriptPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
            {
                // Parse the Python script output: "🏋️ 42"
                var match = Regex.Match(output.Trim(), @"🏋️\s*(\d+)");
                if (match.Success && int.TryParse(match.Groups[1].Value, out int attendance))
                {
                    return new AttendanceResponse
                    {
                        TotalPeopleInGym = attendance,
                        LastRefreshed = DateTime.Now
                    };
                }
            }

            if (!string.IsNullOrEmpty(error))
            {
                Console.WriteLine($"Python script error: {error}");
            }

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error running Python script: {ex.Message}");
            return null;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            _disposed = true;
        }
    }
}