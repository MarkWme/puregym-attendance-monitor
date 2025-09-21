using GymAttendanceMonitor.Services;
using GymAttendanceMonitor.Models;
using System.CommandLine;

#if WINDOWS
using System.Windows.Forms;
#endif

namespace GymAttendance;

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

        var trayOption = new Option<bool>(
            aliases: new[] { "--tray", "-t" },
            description: "Run as Windows tray application");

        var intervalOption = new Option<int>(
            aliases: new[] { "--interval", "-i" },
            getDefaultValue: () => 10,
            description: "Update interval in minutes");

        rootCommand.AddOption(emailOption);
        rootCommand.AddOption(pinOption);
        rootCommand.AddOption(gymOption);
        rootCommand.AddOption(trayOption);
        rootCommand.AddOption(intervalOption);

        rootCommand.SetHandler(async (email, pin, gym, tray, interval) =>
        {
            await RunApplication(email, pin, gym, tray, interval);
        }, emailOption, pinOption, gymOption, trayOption, intervalOption);

        return await rootCommand.InvokeAsync(args);
    }

    static async Task RunApplication(string? email, string? pin, string gym, bool tray, int interval)
    {
        // Get credentials from environment if not provided
        email ??= Environment.GetEnvironmentVariable("PUREGYM_USER");
        pin ??= Environment.GetEnvironmentVariable("PUREGYM_PASS");

        if (tray)
        {
            // Run Windows tray application
            await RunTrayApplication(email, pin, gym, interval);
        }
        else
        {
            // Run command line application
            await RunCommandLineApplication(email, pin, gym, interval);
        }
    }

    static async Task RunCommandLineApplication(string? email, string? pin, string gym, int interval)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pin))
        {
            Console.WriteLine("Error: Email and PIN are required.");
            Console.WriteLine("Provide them via:");
            Console.WriteLine("  Command line: GymAttendance --email <email> --pin <pin>");
            Console.WriteLine("  Environment variables: PUREGYM_USER and PUREGYM_PASS");
            return;
        }

        Console.WriteLine("🏋️  PureGym Attendance Monitor");
        Console.WriteLine("==============================\n");

        using var monitor = new GymMonitorService(email, pin, gym);

        if (await monitor.InitializeAsync())
        {
            if (interval > 0)
            {
                // Continuous monitoring
                monitor.StartMonitoring(TimeSpan.FromMinutes(interval));

                Console.WriteLine("Press Ctrl+C to stop or Enter to check now\n");

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
            Console.WriteLine("Failed to initialize. Please check your credentials.");
        }
    }

    static async Task RunTrayApplication(string? email, string? pin, string gym, int interval)
    {
#if WINDOWS
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pin))
        {
            // Show credential dialog (simplified for now)
            MessageBox.Show("Credentials not provided. Please set PUREGYM_USER and PUREGYM_PASS environment variables or use command line options.",
                          "PureGym Monitor", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var trayApp = new TrayApplication(email, pin, gym, interval);
        Application.Run(trayApp);

        await Task.CompletedTask;
#else
        Console.WriteLine("Tray application is only supported on Windows.");
        await Task.CompletedTask;
#endif
    }
}