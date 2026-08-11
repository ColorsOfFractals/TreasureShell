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

<!-- TREASURESHELL_FRESH_PC -->

## Fresh Windows PC

TreasureShell has been verified from a GitHub ZIP on a separate Windows PC.

Download and extract the repository ZIP, open PowerShell in the repository root, and run:

```powershell
powershell.exe -ExecutionPolicy Bypass -File .\Bootstrap-TreasureShell.ps1
```

The bootstrap verifies or installs:

- Git
- .NET 10 SDK
- nuget.org
- Inno Setup 7

It then restores, builds, publishes, compiles the Inno Setup installer, and verifies the final installer artifact.

Published application:

```text
bin\Release\net10.0-windows\win-x64\publish\TreasureShell.exe
```

Final installer output:

```text
Installer\Output\
```

The repository carries the reproducible build recipe rather than copies of the SDK or Inno Setup binaries.

