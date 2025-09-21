using System.Text;
using System.Text.Json;
using GymAttendanceMonitor.Models;

namespace GymAttendanceMonitor.Services;

public class PureGymApiClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly HttpClientHandler _handler;
    private string? _accessToken;
    private readonly string _email;
    private readonly string _pin;
    private readonly bool _debug;

    private const string AUTH_URL = "https://auth.puregym.com/connect/token";
    private const string API_BASE = "https://capi.puregym.com/api/v2";
    private const string USER_AGENT = "PureGym/7038 CFNetwork/3860.100.1 Darwin/25.0.0";

    public PureGymApiClient(string email, string pin, bool debug = false)
    {
        _email = email;
        _pin = pin;
        _debug = debug;

        // Create HttpClientHandler with automatic decompression and session persistence
        _handler = new HttpClientHandler()
        {
            AutomaticDecompression = System.Net.DecompressionMethods.GZip |
                                   System.Net.DecompressionMethods.Deflate |
                                   System.Net.DecompressionMethods.Brotli,
            UseCookies = true,
            CookieContainer = new System.Net.CookieContainer()
        };

        _httpClient = new HttpClient(_handler);

        // Set headers to match the official iOS app exactly
        _httpClient.DefaultRequestHeaders.Add("User-Agent", USER_AGENT);
        _httpClient.DefaultRequestHeaders.Add("X-PureBrand", "PGUK");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        _httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-GB");
        // Note: Content-Type will be set per request, Accept-Encoding is handled automatically by AutomaticDecompression
    }

    private void DebugWrite(string message)
    {
        if (_debug)
        {
            Console.WriteLine($"DEBUG: {message}");
        }
    }

    public async Task<bool> AuthenticateAsync()
    {
        try
        {
            var formParams = new List<KeyValuePair<string, string>>
            {
                new("grant_type", "password"),
                new("username", _email),
                new("password", _pin),
                new("scope", "pgcapi offline_access"),
                new("client_id", "ro.client")
            };

            var formContent = new FormUrlEncodedContent(formParams);

            // Explicitly set the Content-Type header to match the Python script exactly
            formContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded");

            // Remove the default authorization header for the auth request
            _httpClient.DefaultRequestHeaders.Authorization = null;

            var response = await _httpClient.PostAsync(AUTH_URL, formContent);
            DebugWrite($"Auth Status: {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                DebugWrite($"Auth Error: {errorContent.Substring(0, Math.Min(500, errorContent.Length))}");
                return false;
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            DebugWrite($"Auth Response Length: {jsonResponse.Length}");

            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(jsonResponse);
            DebugWrite($"Token parsed: {tokenResponse?.AccessToken != null}");

            if (tokenResponse?.AccessToken != null)
            {
                _accessToken = tokenResponse.AccessToken;
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            DebugWrite($"Auth Exception: {ex.Message}");
            return false;
        }
    }

    public async Task<List<Gym>> GetGymsAsync()
    {
        if (_accessToken == null)
            throw new InvalidOperationException("Not authenticated");

        try
        {
            var response = await _httpClient.GetAsync($"{API_BASE}/gyms/");

            Console.WriteLine($"DEBUG: Gyms API Status: {response.StatusCode}");

            var jsonResponse = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"DEBUG: Gyms API Response Length: {jsonResponse.Length}");
            Console.WriteLine($"DEBUG: Gyms API Response (first 500 chars): {jsonResponse.Take(500).Aggregate("", (s, c) => s + c)}");

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"DEBUG: Gyms API failed with status {response.StatusCode}");
                return new List<Gym>();
            }

            var gyms = JsonSerializer.Deserialize<List<Gym>>(jsonResponse) ?? new List<Gym>();
            Console.WriteLine($"DEBUG: Deserialized {gyms.Count} gyms");

            return gyms;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DEBUG: Exception in GetGymsAsync: {ex.Message}");
            return new List<Gym>();
        }
    }

    public async Task<Gym?> FindGymByNameAsync(string gymName)
    {
        var gyms = await GetGymsAsync();

        var normalizedSearch = NormalizeGymName(gymName);

        return gyms
            .Select(g => new { Gym = g, Distance = CalculateLevenshteinDistance(normalizedSearch, NormalizeGymName(g.Name)) })
            .OrderBy(x => x.Distance)
            .FirstOrDefault()?.Gym;
    }

    public async Task<AttendanceResponse?> GetAttendanceAsync(int gymId)
    {
        if (_accessToken == null)
            throw new InvalidOperationException("Not authenticated");

        try
        {
            // Use the exact endpoint from the working iOS app
            var response = await _httpClient.GetAsync($"{API_BASE}/gymSessions/gym?gymId={gymId}");

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync();

            // Parse the iOS app response format
            using var document = JsonDocument.Parse(responseContent);
            var root = document.RootElement;

            if (root.TryGetProperty("TotalPeopleInGym", out var totalPeople) &&
                root.TryGetProperty("LastRefreshed", out var lastRefreshed))
            {
                return new AttendanceResponse
                {
                    TotalPeopleInGym = totalPeople.GetInt32(),
                    LastRefreshed = lastRefreshed.GetDateTime()
                };
            }

            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }


    public async Task<Gym?> GetHomeGymAsync()
    {
        if (_accessToken == null)
            throw new InvalidOperationException("Not authenticated");

        try
        {
            var response = await _httpClient.GetAsync($"{API_BASE}/member");
            DebugWrite($"Member API Status: {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                DebugWrite($"Member API Error: {errorContent.Substring(0, Math.Min(500, errorContent.Length))}");
                return null;
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            DebugWrite($"Member API Response Length: {jsonResponse.Length}");
            DebugWrite($"Member API Response (first 1000 chars): {jsonResponse.Substring(0, Math.Min(1000, jsonResponse.Length))}");

            var memberResponse = JsonSerializer.Deserialize<MemberResponse>(jsonResponse);
            DebugWrite($"MemberResponse null: {memberResponse == null}");
            DebugWrite($"HomeGym property null: {memberResponse?.HomeGym == null}");
            if (memberResponse?.HomeGym != null)
            {
                DebugWrite($"Home gym parsed: {memberResponse.HomeGym.Name} (ID: {memberResponse.HomeGym.Id})");
            }
            return memberResponse?.HomeGym;
        }
        catch (Exception ex)
        {
            DebugWrite($"Exception in GetHomeGymAsync: {ex.Message}");
            return null;
        }
    }

    private static string NormalizeGymName(string name)
    {
        return name.ToLowerInvariant().Replace(" ", "").Replace("-", "");
    }

    private static int CalculateLevenshteinDistance(string s1, string s2)
    {
        if (s1.Length == 0) return s2.Length;
        if (s2.Length == 0) return s1.Length;

        var matrix = new int[s1.Length + 1, s2.Length + 1];

        for (int i = 0; i <= s1.Length; i++)
            matrix[i, 0] = i;
        for (int j = 0; j <= s2.Length; j++)
            matrix[0, j] = j;

        for (int i = 1; i <= s1.Length; i++)
        {
            for (int j = 1; j <= s2.Length; j++)
            {
                int cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                matrix[i, j] = Math.Min(Math.Min(
                    matrix[i - 1, j] + 1,
                    matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost);
            }
        }

        return matrix[s1.Length, s2.Length];
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        _handler?.Dispose();
    }
}