using GymAttendanceMonitor.Services;

namespace GymFinder;

static class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("🏋️  PureGym Gym Finder");
        Console.WriteLine("======================\n");

        if (args.Length < 2)
        {
            Console.WriteLine("Usage: dotnet run <email> <pin> [search_term]");
            Console.WriteLine("This will list all gyms or search for gyms containing the search term");
            return;
        }

        string email = args[0];
        string pin = args[1];
        string? searchTerm = args.Length > 2 ? args[2].ToLowerInvariant() : null;

        try
        {
            using var apiClient = new PureGymApiClient(email, pin);

            Console.Write("Authenticating... ");
            if (await apiClient.AuthenticateAsync())
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓ Success");
                Console.ResetColor();

                Console.WriteLine("Fetching gym list...");
                var gyms = await apiClient.GetGymsAsync();

                Console.WriteLine("\nTrying to get home gym...");
                var homeGym = await apiClient.GetHomeGymAsync();

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ Found {gyms.Count} gyms from list");
                if (homeGym != null)
                {
                    Console.WriteLine($"✓ Home gym: {homeGym.Name} (ID: {homeGym.Id})");
                }
                Console.ResetColor();

                Console.WriteLine();

                var filteredGyms = gyms;
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    filteredGyms = gyms.Where(g => g.Name.ToLowerInvariant().Contains(searchTerm)).ToList();
                    Console.WriteLine($"Gyms containing '{searchTerm}':");
                }
                else
                {
                    Console.WriteLine("All PureGym locations:");
                }

                Console.WriteLine(new string('-', 60));

                if (filteredGyms.Any())
                {
                    foreach (var gym in filteredGyms.OrderBy(g => g.Name))
                    {
                        Console.WriteLine($"ID: {gym.Id,-6} Name: {gym.Name}");
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"No gyms found containing '{searchTerm}'");
                    Console.ResetColor();

                    // Show Canterbury gyms as suggestions
                    var canterburyGyms = gyms.Where(g => g.Name.ToLowerInvariant().Contains("canterbury")).ToList();
                    if (canterburyGyms.Any())
                    {
                        Console.WriteLine("\nDid you mean one of these Canterbury gyms?");
                        foreach (var gym in canterburyGyms)
                        {
                            Console.WriteLine($"ID: {gym.Id,-6} Name: {gym.Name}");
                        }
                    }
                }

                Console.WriteLine(new string('-', 60));
                Console.WriteLine($"Total: {filteredGyms.Count} gym(s)");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("✗ Authentication failed");
                Console.ResetColor();
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {ex.Message}");
            Console.ResetColor();
        }
    }
}