using GymAttendanceMonitor.Services;
using GymAttendanceMonitor.Models;
using System.CommandLine;

namespace GymAttendanceCLI;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("PureGym Attendance Monitor")
        {
            TreatUnmatchedTokensAsErrors = false
        };

        // Add options
        var emailOption = new Option<string?>(
            aliases: new[] { "--email", "-e" },
            description: "PureGym email address");

        var pinOption = new Option<string?>(
            aliases: new[] { "--pin", "-p" },
            description: "PureGym PIN");

        var gymOption = new Option<string>(
            aliases: new[] { "--gym", "-g" },
            getDefaultValue: () => "home",
            description: "Gym name or 'home' for your home gym");

        var intervalOption = new Option<int>(
            aliases: new[] { "--interval", "-i" },
            getDefaultValue: () => 10,
            description: "Update interval in minutes (0 for single check)");

        var debugOption = new Option<bool>(
            aliases: new[] { "--debug", "-d" },
            description: "Enable debug output");

        rootCommand.AddOption(emailOption);
        rootCommand.AddOption(pinOption);
        rootCommand.AddOption(gymOption);
        rootCommand.AddOption(intervalOption);
        rootCommand.AddOption(debugOption);

        rootCommand.SetHandler(async (email, pin, gym, interval, debug) =>
        {
            await RunApplication(email, pin, gym, interval, debug);
        }, emailOption, pinOption, gymOption, intervalOption, debugOption);

        return await rootCommand.InvokeAsync(args);
    }

    static async Task RunApplication(string? email, string? pin, string gym, int interval, bool debug)
    {
        // Get credentials from environment if not provided
        email ??= Environment.GetEnvironmentVariable("PUREGYM_USER");
        pin ??= Environment.GetEnvironmentVariable("PUREGYM_PASS");

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pin))
        {
            Console.WriteLine("Error: Email and PIN are required.");
            Console.WriteLine("Provide them via:");
            Console.WriteLine("  Command line: GymAttendance --email <email> --pin <pin>");
            Console.WriteLine("  Environment variables: PUREGYM_USER and PUREGYM_PASS");
            return;
        }

        Console.WriteLine("🏋️  PureGym Attendance Monitor");
        Console.WriteLine("==============================");

        using var monitor = new GymMonitorService(email, pin, gym, debug);

        if (await monitor.InitializeAsync())
        {
            if (interval > 0)
            {
                // Continuous monitoring
                monitor.StartMonitoring(TimeSpan.FromMinutes(interval));

                Console.WriteLine($"\nMonitoring every {interval} minutes. Press Ctrl+C to stop or Enter to check now.\n");

                // Handle manual checks and exit
                var cancellationTokenSource = new CancellationTokenSource();
                Console.CancelKeyPress += (_, e) =>
                {
                    e.Cancel = true;
                    cancellationTokenSource.Cancel();
                };

                var readTask = Task.Run(async () =>
                {
                    while (!cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        try
                        {
                            await Task.Delay(100, cancellationTokenSource.Token);
                            if (Console.KeyAvailable)
                            {
                                var key = Console.ReadKey(true);
                                if (key.Key == ConsoleKey.Enter)
                                {
                                    await monitor.DisplayAttendanceAsync();
                                }
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                    }
                });

                try
                {
                    await readTask;
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancelled
                }

                Console.WriteLine("\nStopping monitor...");
            }
            else
            {
                // Single check
                await monitor.DisplayAttendanceAsync();
            }
        }
        else
        {
            Console.WriteLine("Failed to initialize. Please check your credentials and try again.");
        }
    }
}