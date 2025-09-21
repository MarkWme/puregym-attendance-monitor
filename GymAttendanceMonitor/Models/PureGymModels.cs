using System.Text.Json.Serialization;

namespace GymAttendanceMonitor.Models;

public class TokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = string.Empty;

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
}

public class Gym
{
    [JsonPropertyName("Id")]
    public int Id { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("Status")]
    public string Status { get; set; } = string.Empty;
}

public class AttendanceResponse
{
    [JsonPropertyName("totalPeopleInGym")]
    public int TotalPeopleInGym { get; set; }

    [JsonPropertyName("lastRefreshed")]
    public DateTime LastRefreshed { get; set; }
}

public class MemberResponse
{
    [JsonPropertyName("HomeGym")]
    public Gym? HomeGym { get; set; }

    [JsonPropertyName("homeGymId")]
    public int HomeGymId { get; set; }
}

public class AttendanceLevel
{
    public string Description { get; set; } = string.Empty;
    public ConsoleColor Color { get; set; }

    public static AttendanceLevel GetLevel(int attendance)
    {
        return attendance switch
        {
            <= 20 => new AttendanceLevel { Description = "Light", Color = ConsoleColor.Green },
            <= 40 => new AttendanceLevel { Description = "Moderate", Color = ConsoleColor.Yellow },
            <= 60 => new AttendanceLevel { Description = "Busy", Color = ConsoleColor.Red },
            _ => new AttendanceLevel { Description = "Very Busy", Color = ConsoleColor.DarkRed }
        };
    }
}