using GymAttendanceMonitor.Services;
using GymAttendanceMonitor.Models;
using System.CommandLine;
using System.Windows.Forms;

namespace GymAttendanceTray;

class Program
{
    [STAThread]
    static async Task<int> Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var rootCommand = new RootCommand("PureGym Attendance Monitor - Windows Tray")
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
            description: "Update interval in minutes");

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
            await RunTrayApplication(email, pin, gym, interval, debug);
        }, emailOption, pinOption, gymOption, intervalOption, debugOption);

        return await rootCommand.InvokeAsync(args);
    }

    static async Task RunTrayApplication(string? email, string? pin, string gym, int interval, bool debug)
    {
        // Get credentials from environment if not provided
        email ??= Environment.GetEnvironmentVariable("PUREGYM_USER");
        pin ??= Environment.GetEnvironmentVariable("PUREGYM_PASS");

        // If still no credentials, show login dialog
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pin))
        {
            var loginForm = new LoginForm();
            if (loginForm.ShowDialog() == DialogResult.OK)
            {
                email = loginForm.Email;
                pin = loginForm.Pin;
            }
            else
            {
                return; // User cancelled
            }
        }

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pin))
        {
            MessageBox.Show("Email and PIN are required to run the tray application.",
                          "PureGym Monitor", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var trayApp = new TrayApplication(email, pin, gym, interval, debug);
        Application.Run(trayApp);

        await Task.CompletedTask;
    }
}