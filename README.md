# PureGym Attendance Monitor

A Windows application suite for monitoring PureGym attendance in real-time. Track gym capacity with color-coded indicators and system tray integration.

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![.NET](https://img.shields.io/badge/.NET-9.0-purple.svg)
![Platform](https://img.shields.io/badge/platform-Windows-blue.svg)

## Features

- **Real-time Monitoring**: Check current gym attendance every 10-15 minutes
- **Color-coded Indicators**: Visual attendance levels (🟢 Quiet, 🟡 Moderate, 🔴 Busy, 🔴 Very Busy)
- **System Tray Integration**: Unobtrusive monitoring with hover details
- **Secure Credential Storage**: Uses Windows Credential Manager for safe authentication
- **Multiple Interfaces**: Both CLI and GUI applications available
- **Debug Mode**: Troubleshooting support with verbose output

## Applications

### 🖥️ GymAttendanceTray
Windows system tray application with persistent monitoring and visual indicators.

**Features:**
- System tray icon showing current attendance count
- Color-coded background based on gym capacity
- Context menu with manual refresh and detailed view
- Secure login dialog with credential persistence
- Balloon notifications for status updates

### 💻 GymAttendanceCLI
Command-line interface for one-time attendance checks or scripting.

**Features:**
- Simple command-line output
- Environment variable support
- Debug mode for troubleshooting
- Scriptable for automation

### 📚 GymAttendanceMonitor
Core library containing the PureGym API client and shared models.

## Installation

### Prerequisites
- Windows 10/11
- .NET 9.0 Runtime

### Quick Start

1. **Download the latest release** from the [Releases](https://github.com/MarkWme/puregym-attendance-monitor/releases) page

2. **For Tray Application:**
   ```bash
   GymAttendanceTray.exe
   ```
   - First run will prompt for PureGym credentials
   - Choose "Save credentials securely" to remember login

3. **For CLI Application:**
   ```bash
   GymAttendanceCLI.exe --email your.email@domain.com --pin 1234
   ```

## Usage

### Tray Application

Launch the tray application:
```bash
GymAttendanceTray.exe [options]
```

**Options:**
- `--email, -e`: PureGym email address
- `--pin, -p`: PureGym PIN
- `--gym, -g`: Gym name (default: "home" for your home gym)
- `--interval, -i`: Update interval in minutes (default: 10)
- `--debug, -d`: Enable debug output

**Example:**
```bash
GymAttendanceTray.exe --interval 5 --debug
```

### CLI Application

Check attendance once:
```bash
GymAttendanceCLI.exe [options]
```

**Options:**
- `--email, -e`: PureGym email address
- `--pin, -p`: PureGym PIN
- `--gym, -g`: Gym name (default: "home")
- `--interval, -i`: Continuous monitoring interval in minutes
- `--debug, -d`: Enable debug output

**Examples:**
```bash
# Single check
GymAttendanceCLI.exe --email user@domain.com --pin 1234

# Continuous monitoring every 5 minutes
GymAttendanceCLI.exe --interval 5

# Using environment variables
set PUREGYM_USER=user@domain.com
set PUREGYM_PASS=1234
GymAttendanceCLI.exe
```

### Environment Variables

Both applications support environment variables for credentials:
- `PUREGYM_USER`: Your PureGym email address
- `PUREGYM_PASS`: Your PureGym PIN

## Attendance Levels

The application uses color-coded indicators to show gym capacity:

| Range | Level | Color | Description |
|-------|-------|-------|-------------|
| 0-20 | 🟢 Quiet | Green | Low attendance, plenty of space |
| 21-40 | 🟡 Moderate | Yellow | Moderate attendance, some equipment may be busy |
| 41-60 | 🔴 Busy | Red | High attendance, expect queues for popular equipment |
| 61+ | 🔴 Very Busy | Dark Red | Very high attendance, significant wait times likely |

## Building from Source

### Prerequisites
- .NET 9.0 SDK
- Windows 10/11 (for Windows Forms components)

### Build Steps

1. **Clone the repository:**
   ```bash
   git clone https://github.com/MarkWme/puregym-attendance-monitor.git
   cd puregym-attendance-monitor
   ```

2. **Restore dependencies:**
   ```bash
   dotnet restore
   ```

3. **Build all projects:**
   ```bash
   dotnet build --configuration Release
   ```

4. **Build specific applications:**
   ```bash
   # CLI Application
   dotnet build GymAttendanceCLI --configuration Release

   # Tray Application
   dotnet build GymAttendanceTray --configuration Release
   ```

5. **Create self-contained executables:**
   ```bash
   # CLI Application
   dotnet publish GymAttendanceCLI -c Release -r win-x64 --self-contained

   # Tray Application
   dotnet publish GymAttendanceTray -c Release -r win-x64 --self-contained
   ```

## Security

- **No Hardcoded Credentials**: All authentication uses runtime-provided credentials
- **Secure Storage**: Windows Credential Manager integration for persistent storage
- **HTTPS Only**: All API communication uses encrypted connections
- **OAuth 2.0**: Follows PureGym's official authentication flow

## Troubleshooting

### Common Issues

**"Failed to connect. Check credentials."**
- Verify your PureGym email and PIN are correct
- Ensure you can log in to the PureGym website/app
- Try running with `--debug` flag for detailed error information

**"Update failed" errors**
- Check your internet connection
- PureGym servers may be temporarily unavailable
- Use debug mode to see specific error details

**System tray icon not appearing**
- Ensure Windows system tray is enabled
- Check if the application is running in Task Manager
- Try restarting as administrator

### Debug Mode

Enable debug mode for detailed logging:
```bash
GymAttendanceTray.exe --debug
GymAttendanceCLI.exe --debug
```

This provides verbose output including:
- HTTP request/response details
- Authentication flow information
- API endpoint responses
- Error stack traces

## API Information

This application uses PureGym's mobile API endpoints:
- **Authentication**: OAuth 2.0 with email/PIN
- **Member Info**: Retrieves home gym information
- **Attendance**: Real-time gym capacity data
- **Rate Limiting**: Respectful polling intervals (10+ minutes recommended)

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Disclaimer

This application is not affiliated with PureGym. It uses publicly available API endpoints for personal monitoring purposes. Please use responsibly and in accordance with PureGym's terms of service.

## Support

For issues, questions, or feature requests, please [open an issue](https://github.com/MarkWme/puregym-attendance-monitor/issues) on GitHub.