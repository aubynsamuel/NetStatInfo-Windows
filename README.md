# NetStatInfoWin

NetStatInfoWin is a simple Windows desktop app that shows how much network data each app has used. It displays sent, received, and total data for the current session, the last hour, the last six hours, or today.

The app reads usage data reported by Windows and refreshes it every 30 seconds. You can also refresh it manually or press `F5`.

## Features

- Per-app network usage
- Sent and received data totals
- Session, last hour, last six hours, and today views
- Automatic and manual refresh
- Separate system and unattributed usage

## Requirements

- Windows 10 version 1809 or later
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Windows Developer Mode enabled

## Run the app

Clone the repository, open PowerShell in the project folder, and run:

```powershell
$platform = if ($env:PROCESSOR_ARCHITECTURE -eq 'AMD64') { 'x64' } else { $env:PROCESSOR_ARCHITECTURE }
dotnet run --project .\NetStatInfoWin\NetStatInfoWin.csproj -c Debug -p:Platform=$platform
```

The app must run as a packaged Windows app because the network usage API requires package identity and the `networkDataPlanProvisioning` capability. The run command handles the development package registration automatically.

## Run the tests

```powershell
$platform = if ($env:PROCESSOR_ARCHITECTURE -eq 'AMD64') { 'x64' } else { $env:PROCESSOR_ARCHITECTURE }
dotnet test .\NetStatInfoWin.Tests\NetStatInfoWin.Tests.csproj -c Debug -p:Platform=$platform
```

## Built with

- C# and .NET 10
- WinUI 3
- Windows App SDK
- MSTest

Windows may report usage with a short delay, so recent activity might not appear immediately.
