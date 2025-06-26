# PortProxy Agent Installer Build Script
param(
    [string]$Configuration = "Release",
    [string]$OutputDir = ".\bin\$Configuration",
    [string]$Version = "1.0.0"
)

Write-Host "Building PortProxy Agent Installer..." -ForegroundColor Green

# Ensure WiX is installed
$wixPath = "${env:ProgramFiles(x86)}\WiX Toolset v3.11\bin"
if (-not (Test-Path $wixPath)) {
    Write-Error "WiX Toolset v3.11 is required. Please install from https://wixtoolset.org/"
    exit 1
}

# Add WiX to PATH
$env:PATH = "$wixPath;$env:PATH"

try {
    # Build the agent project first
    Write-Host "Building PortProxy Agent..." -ForegroundColor Yellow
    dotnet build "..\PortProxyAgent\PortProxyAgent.csproj" -c $Configuration
    if ($LASTEXITCODE -ne 0) { throw "Agent build failed" }

    # Publish the agent (self-contained for easier deployment)
    Write-Host "Publishing PortProxy Agent..." -ForegroundColor Yellow
    dotnet publish "..\PortProxyAgent\PortProxyAgent.csproj" -c $Configuration -o "..\PortProxyAgent\bin\$Configuration\publish" --self-contained false
    if ($LASTEXITCODE -ne 0) { throw "Agent publish failed" }

    # Build the installer
    Write-Host "Building installer..." -ForegroundColor Yellow
    msbuild "PortProxyAgent.Installer.wixproj" /p:Configuration=$Configuration /p:Version=$Version
    if ($LASTEXITCODE -ne 0) { throw "Installer build failed" }

    Write-Host "Installer built successfully!" -ForegroundColor Green
    Write-Host "Output: $OutputDir\PortProxyAgent.msi" -ForegroundColor Cyan

    # Optional: Sign the installer
    if ($env:SIGNTOOL_CERT) {
        Write-Host "Signing installer..." -ForegroundColor Yellow
        signtool sign /f $env:SIGNTOOL_CERT /p $env:SIGNTOOL_PASSWORD /t http://timestamp.comodoca.com "$OutputDir\PortProxyAgent.msi"
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Installer signed successfully!" -ForegroundColor Green
        } else {
            Write-Warning "Failed to sign installer"
        }
    }
}
catch {
    Write-Error "Build failed: $_"
    exit 1
}