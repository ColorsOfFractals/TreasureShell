# TreasureShell
Treasure Chest Themed Front-End shell for PowerShell.

---

## Running TreasureShell

TreasureShell is published as a self-contained, single-file Windows x64 application.

The published application includes the required .NET runtime, so a separate .NET runtime installation is not required to run TreasureShell.

## Development Setup

Development uses:

- Windows x64
- PowerShell
- Git
- .NET 10 SDK
- HtmlAgilityPack via NuGet

Run:

    .\Setup-TreasureShell.ps1

The setup path is:

    dependency check
          |
          v
       restore
          |
          v
        build
          |
          v
       publish

Published application:

    bin\Release\net10.0-windows\win-x64\publish\TreasureShell.exe

### Dr. Recursion

    Teacher
       |
       v
    README + Setup
       |
       v
    Restore
       |
       v
    Build
       |
       v
    Publish

Don't just draw the fractal. Ride the spiral.
