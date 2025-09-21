using GymAttendanceMonitor.Services;

namespace GymAttendanceMonitor;

static class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("🏋️  PureGym Attendance Monitor");
        Console.WriteLine("==============================\n");

        if (args.Length < 2)
        {
            Console.WriteLine("Usage: dotnet run <email> <pin> [gym_name]");
            Console.WriteLine("Example: dotnet run \"your.email@domain.com\" \"1234\" \"Canterbury Wincheap\"");
            return;
        }

        string email = args[0];
        string pin = args[1];
        string gymName = args.Length > 2 ? args[2] : "Canterbury Wincheap";

        Console.WriteLine($"Email: {email}");
        Console.WriteLine($"PIN: {new string('*', pin.Length)}");
        Console.WriteLine($"Gym: {gymName}\n");

        try
        {
            using var monitor = new GymMonitorService(email, pin, gymName);

            if (await monitor.InitializeAsync())
            {
                Console.WriteLine();
                monitor.StartMonitoring(TimeSpan.FromMinutes(10));

                var cancellationTokenSource = new CancellationTokenSource();
                Console.CancelKeyPress += (s, e) =>
                {
                    e.Cancel = true;
                    cancellationTokenSource.Cancel();
                };

                try
                {
                    while (!cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        var task = Task.Run(() => Console.ReadLine(), cancellationTokenSource.Token);

                        try
                        {
                            await task;
                            if (!cancellationTokenSource.Token.IsCancellationRequested)
                            {
                                await monitor.DisplayAttendanceAsync();
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                }

                Console.WriteLine("\nShutting down...");
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"An error occurred: {ex.Message}");
            Console.ResetColor();
        }
    }
}
