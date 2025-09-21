# PureGym Attendance Monitor

A .NET console application to monitor gym attendance at your local PureGym.

## Features

- **Real-time Attendance Monitoring**: Polls PureGym's API for current attendance figures
- **Automatic Gym Discovery**: Finds your gym by name using fuzzy matching
- **Color-coded Status**: Visual indicators for attendance levels:
  - 🟢 **Light** (0-20 people): Green
  - 🟡 **Moderate** (21-40 people): Yellow
  - 🔴 **Busy** (41-60 people): Red
  - 🔴 **Very Busy** (60+ people): Dark Red
- **Configurable Polling**: Default 10-minute intervals
- **Manual Checks**: Press Enter to check attendance immediately

## Prerequisites

- .NET 9.0 or later
- PureGym membership with email and PIN

## Usage

### Console Version (Cross-platform)

```bash
# Clone or download the project
cd GymAttendanceMonitor

# Run the application
dotnet run "your.email@domain.com" "your-pin" "Canterbury Wincheap"

# Or build and run the executable
dotnet build
dotnet run -- "your.email@domain.com" "your-pin" "Canterbury Wincheap"
```

### Windows Taskbar Version

For Windows users wanting a taskbar application, you'll need to:

1. Change the project to target `net9.0-windows` in the `.csproj` file
2. Add `<UseWindowsForms>true</UseWindowsForms>` to the PropertyGroup
3. Re-enable the Windows Forms code in the `Forms` directory
4. Build and run on Windows

## Configuration

### Command Line Arguments

1. **Email**: Your PureGym account email (required)
2. **PIN**: Your PureGym PIN (required)
3. **Gym Name**: Name of your gym (optional, defaults to "Canterbury Wincheap")

### Gym Name Examples

The application uses fuzzy matching to find your gym. You can use variations like:
- "Canterbury Wincheap"
- "canterbury wincheap"
- "Canterbury"
- "Wincheap"

## How It Works

1. **Authentication**: Connects to PureGym's OAuth API using your credentials
2. **Gym Discovery**: Searches for your gym using the name provided
3. **Monitoring**: Polls the attendance API every 10 minutes
4. **Display**: Shows current attendance with color-coded status

## Security Notes

- Your credentials are only used to authenticate with PureGym's official API
- No data is stored or transmitted to third parties
- The application runs locally on your machine

## Troubleshooting

### Authentication Issues
- Verify your email and PIN are correct
- Ensure you can log into the PureGym app/website

### Gym Not Found
- Try different variations of your gym name
- Check the exact name on PureGym's website
- Use partial names (e.g., just "Canterbury")

### Connection Issues
- Check your internet connection
- PureGym's API may be temporarily unavailable

## API Information

This application uses PureGym's mobile API endpoints:
- Authentication: `https://auth.puregym.com/connect/token`
- Gym List: `https://capi.puregym.com/api/v1/gyms/`
- Attendance: `https://capi.puregym.com/api/v1/gyms/{id}/attendance`

**Note**: This is an unofficial application using undocumented APIs that may change without notice.

## License

This project is for educational purposes. Please use responsibly and in accordance with PureGym's terms of service.