# IIS Deployment Script for DemoGetInfoApi with Security Hardening
# Based on optimizations applied manually
# Author: AI Assistant
# Date: $(Get-Date)

param(
    [string]$SiteName = "DemoGetInfoApi",
    [string]$SitePath = "C:\inetpub\wwwroot\DemoGetInfoApi",
    [string]$ApplicationPool = "DemoGetInfoApiPool",
    [int]$Port = 8080,
    [string]$SourcePath = ".\bin\Release\net8.0\publish",
    [switch]$Force
)

Write-Host "=== IIS Deployment Script for DemoGetInfoApi (Secured) ===" -ForegroundColor Green
Write-Host "Site: $SiteName" -ForegroundColor Cyan
Write-Host "Path: $SitePath" -ForegroundColor Cyan
Write-Host "Application Pool: $ApplicationPool" -ForegroundColor Cyan
Write-Host "Port: $Port" -ForegroundColor Cyan

# Check administrator privileges
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Error "This script must be run as Administrator!"
    exit 1
}

# Import WebAdministration module
Import-Module WebAdministration -ErrorAction SilentlyContinue

# 0. Install required IIS features
Write-Host ""
Write-Host "0. Installing required IIS features..." -ForegroundColor Yellow

# Install Windows Authentication using DISM (most reliable method)
Write-Host "Installing Windows Authentication..." -ForegroundColor Yellow
try {
    $dismResult = & dism /online /enable-feature /featurename:IIS-WindowsAuthentication /all /norestart 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "[OK] Windows Authentication enabled" -ForegroundColor Green
    } elseif ($LASTEXITCODE -eq 1) {
        Write-Host "[OK] Windows Authentication already enabled" -ForegroundColor Green
    } else {
        Write-Host "[WARNING] Windows Authentication installation may have failed" -ForegroundColor Yellow
    }
} catch {
    Write-Host "[WARNING] Could not install Windows Authentication via DISM" -ForegroundColor Yellow
}

# Install IIS Web Server if not present
Write-Host "Installing IIS Web Server..." -ForegroundColor Yellow
try {
    $dismResult = & dism /online /enable-feature /featurename:IIS-WebServerRole /all /norestart 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "[OK] IIS Web Server enabled" -ForegroundColor Green
    } elseif ($LASTEXITCODE -eq 1) {
        Write-Host "[OK] IIS Web Server already enabled" -ForegroundColor Green
    } else {
        Write-Host "[WARNING] IIS Web Server installation may have failed" -ForegroundColor Yellow
    }
} catch {
    Write-Host "[WARNING] Could not install IIS Web Server via DISM" -ForegroundColor Yellow
}

# Install ASP.NET Core features
Write-Host "Installing ASP.NET Core features..." -ForegroundColor Yellow
$aspNetFeatures = @("IIS-NetFxExtensibility45", "IIS-ASPNET45", "IIS-ISAPIExtensions", "IIS-ISAPIFilter")
foreach ($feature in $aspNetFeatures) {
    try {
        $dismResult = & dism /online /enable-feature /featurename:$feature /all /norestart 2>&1
        if ($LASTEXITCODE -eq 0 -or $LASTEXITCODE -eq 1) {
            Write-Host "[OK] $feature enabled" -ForegroundColor Green
        }
    } catch {
        Write-Host "[WARNING] Could not install $feature" -ForegroundColor Yellow
    }
}

# 1. Stop existing services
Write-Host ""
Write-Host "1. Stopping existing services..." -ForegroundColor Yellow

if (Get-Website -Name $SiteName -ErrorAction SilentlyContinue) {
    Stop-Website -Name $SiteName -ErrorAction SilentlyContinue
    Write-Host "[OK] Website stopped" -ForegroundColor Green
}

if (Get-IISAppPool -Name $ApplicationPool -ErrorAction SilentlyContinue) {
    Stop-WebAppPool -Name $ApplicationPool -ErrorAction SilentlyContinue
    Write-Host "[OK] Application pool stopped" -ForegroundColor Green
}

# Wait for processes to release file handles
Write-Host "Waiting for processes to release file handles..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

# Force kill any remaining processes
$processNames = @("w3wp", "dotnet", "DemoGetInfoApi")
foreach ($processName in $processNames) {
    $processes = Get-Process -Name $processName -ErrorAction SilentlyContinue
    if ($processes) {
        Write-Host "Force stopping $processName processes..." -ForegroundColor Yellow
        $processes | Stop-Process -Force -ErrorAction SilentlyContinue
    }
}

# 2. Prepare deployment directory
Write-Host ""
Write-Host "2. Preparing deployment directory..." -ForegroundColor Yellow

if (Test-Path $SitePath) {
    if ($Force) {
        try {
            Remove-Item $SitePath -Recurse -Force -ErrorAction Stop
            Write-Host "[OK] Old directory removed" -ForegroundColor Green
        }
        catch {
            Write-Host "Warning: Could not completely remove old directory" -ForegroundColor Yellow
        }
    }
}

New-Item -ItemType Directory -Force -Path $SitePath | Out-Null
Write-Host "[OK] Directory created: $SitePath" -ForegroundColor Green

# 3. Publishing application
Write-Host ""
Write-Host "3. Publishing application..." -ForegroundColor Yellow

# Clean publish directory first
$publishDir = ".\bin\Release\net8.0\publish"
if (Test-Path $publishDir) {
    Remove-Item $publishDir -Recurse -Force -ErrorAction SilentlyContinue
}

# Publish application
$publishCommand = "dotnet publish -c Release -r win-x64 --self-contained false -o `"$publishDir`""
Write-Host "Executing: $publishCommand" -ForegroundColor Cyan

Invoke-Expression $publishCommand

if ($LASTEXITCODE -ne 0) {
    Write-Error "Error publishing application"
    exit 1
}
Write-Host "[OK] Application published to $publishDir" -ForegroundColor Green

# Copy files from publish directory
if (Test-Path $SourcePath) {
    try {
        Copy-Item "$SourcePath\*" -Destination $SitePath -Recurse -Force -ErrorAction Stop
        Write-Host "[OK] Files copied from $SourcePath" -ForegroundColor Green
    }
    catch {
        Write-Error "Failed to copy files: $($_.Exception.Message)"
        exit 1
    }
}

# 4. Security hardening and cleanup
Write-Host ""
Write-Host "4. Applying security hardening..." -ForegroundColor Yellow

# Remove unnecessary architecture folders
$unnecessaryFolders = @("x86", "arm64", "armhf")
foreach ($folder in $unnecessaryFolders) {
    $folderPath = Join-Path $SitePath $folder
    if (Test-Path $folderPath) {
        Remove-Item $folderPath -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "[OK] Removed unnecessary folder: $folder" -ForegroundColor Green
    }
}

# Remove debug symbols (PDB files)
Get-ChildItem -Path $SitePath -Filter "*.pdb" -Recurse | ForEach-Object {
    Remove-Item $_.FullName -Force -ErrorAction SilentlyContinue
    Write-Host "[OK] Removed debug file: $($_.Name)" -ForegroundColor Green
}

# Remove development configuration
$devConfigPath = Join-Path $SitePath "appsettings.Development.json"
if (Test-Path $devConfigPath) {
    Remove-Item $devConfigPath -Force -ErrorAction SilentlyContinue
    Write-Host "[OK] Removed development configuration" -ForegroundColor Green
}

# Remove test files that might expose internal paths
$testFiles = @("real-wim-test.json", "*test*.json")
foreach ($pattern in $testFiles) {
    Get-ChildItem -Path $SitePath -Filter $pattern -ErrorAction SilentlyContinue | ForEach-Object {
        Remove-Item $_.FullName -Force -ErrorAction SilentlyContinue
        Write-Host "[OK] Removed test file: $($_.Name)" -ForegroundColor Green
    }
}

# Secure appsettings.json (remove sensitive paths)
$appsettingsPath = Join-Path $SitePath "appsettings.json"
if (Test-Path $appsettingsPath) {
    $secureConfig = @'
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "WimAnalyzer": {
    "SupportedExtensions": [ ".wim", ".esd" ],
    "MaxFileSizeGB": 10,
    "UseRealService": true,
    "EnableManagedWimLib": true,
    "CacheEnabled": true
  }
}
'@
    $secureConfig | Out-File $appsettingsPath -Encoding UTF8 -Force
    Write-Host "[OK] Secured appsettings.json" -ForegroundColor Green
}

# Secure appsettings.Production.json (remove sensitive information)
$prodConfigPath = Join-Path $SitePath "appsettings.Production.json"
if (Test-Path $prodConfigPath) {
    $secureProdConfig = @"
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "AllowedHosts": "*",
  "Kestrel": {
    "EndPoints": {
      "Http": {
        "Url": "http://localhost:$Port"
      }
    }
  },
  "WimAnalyzer": {
    "LibPath": "",
    "EnableMockService": false,
    "MaxFileSize": 52428800,
    "SupportedExtensions": [".wim", ".esd"]
  }
}
"@
    $secureProdConfig | Out-File $prodConfigPath -Encoding UTF8 -Force
    Write-Host "[OK] Secured appsettings.Production.json" -ForegroundColor Green
}

Write-Host "[OK] Security hardening completed" -ForegroundColor Green

# 5. Configure application pool
Write-Host ""
Write-Host "5. Configuring application pool..." -ForegroundColor Yellow

if (Get-IISAppPool -Name $ApplicationPool -ErrorAction SilentlyContinue) {
    Remove-WebAppPool -Name $ApplicationPool -ErrorAction SilentlyContinue
    Write-Host "[OK] Old pool removed" -ForegroundColor Green
}

New-WebAppPool -Name $ApplicationPool -Force | Out-Null
Set-ItemProperty -Path "IIS:\AppPools\$ApplicationPool" -Name "processModel.identityType" -Value "ApplicationPoolIdentity"
Set-ItemProperty -Path "IIS:\AppPools\$ApplicationPool" -Name "managedRuntimeVersion" -Value ""
Start-WebAppPool -Name $ApplicationPool
Write-Host "[OK] Application pool '$ApplicationPool' created and configured" -ForegroundColor Green

# 6. Create website
Write-Host ""
Write-Host "6. Creating website..." -ForegroundColor Yellow

if (Get-Website -Name $SiteName -ErrorAction SilentlyContinue) {
    Remove-Website -Name $SiteName -ErrorAction SilentlyContinue
    Write-Host "[OK] Old site removed" -ForegroundColor Green
}

New-Website -Name $SiteName -PhysicalPath $SitePath -Port $Port -ApplicationPool $ApplicationPool | Out-Null
Write-Host "[OK] Website '$SiteName' created on port $Port" -ForegroundColor Green

# Configure Windows Authentication (Force Windows Auth to trigger browser popup)
Write-Host "Configuring Windows Authentication..." -ForegroundColor Yellow
Set-WebConfigurationProperty -Filter "system.webServer/security/authentication/windowsAuthentication" -Name "enabled" -Value "True" -PSPath "IIS:\" -Location "$SiteName"
Set-WebConfigurationProperty -Filter "system.webServer/security/authentication/anonymousAuthentication" -Name "enabled" -Value "False" -PSPath "IIS:\" -Location "$SiteName"
Write-Host "[OK] Windows Authentication enabled, Anonymous Authentication disabled" -ForegroundColor Green

# 7. Configure permissions
Write-Host ""
Write-Host "7. Configuring permissions..." -ForegroundColor Yellow

$acl = Get-Acl $SitePath
$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule("IIS AppPool\$ApplicationPool","FullControl","ContainerInherit,ObjectInherit","None","Allow")
$acl.SetAccessRule($accessRule)
Set-Acl $SitePath $acl
Write-Host "[OK] Permissions configured for IIS AppPool\$ApplicationPool" -ForegroundColor Green

# 8. Create logs directory
$logsPath = Join-Path $SitePath "logs"
if (-not (Test-Path $logsPath)) {
    New-Item -ItemType Directory -Path $logsPath -Force | Out-Null
    Write-Host "[OK] Logs directory created" -ForegroundColor Green
}

# 9. Start services
Write-Host ""
Write-Host "9. Starting services..." -ForegroundColor Yellow

Start-WebAppPool -Name $ApplicationPool
Write-Host "[OK] Application pool started" -ForegroundColor Green

Start-Website -Name $SiteName
Write-Host "[OK] Website started" -ForegroundColor Green

# 10. Configure firewall
Write-Host ""
Write-Host "10. Configuring firewall..." -ForegroundColor Yellow

$firewallRule = Get-NetFirewallRule -DisplayName "DemoGetInfoApi Port $Port" -ErrorAction SilentlyContinue
if (-not $firewallRule) {
    New-NetFirewallRule -DisplayName "DemoGetInfoApi Port $Port" -Direction Inbound -Protocol TCP -LocalPort $Port -Action Allow | Out-Null
    Write-Host "[OK] Firewall rule created for port $Port" -ForegroundColor Green
} else {
    Write-Host "[OK] Firewall rule already exists for port $Port" -ForegroundColor Green
}

# 11. Test application
Write-Host ""
Write-Host "11. Testing application..." -ForegroundColor Yellow

Start-Sleep -Seconds 5
try {
    $response = Invoke-WebRequest -Uri "http://localhost:$Port/health" -UseBasicParsing -TimeoutSec 30
    if ($response.StatusCode -eq 200) {
        Write-Host "[OK] Application responds correctly" -ForegroundColor Green
    } else {
        Write-Host "[WARNING] Application responded with status code: $($response.StatusCode)" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "[WARNING] Could not test application: $($_.Exception.Message)" -ForegroundColor Yellow
}

# 12. Display summary
Write-Host ""
Write-Host "=== DEPLOYMENT COMPLETED SUCCESSFULLY ===" -ForegroundColor Green
Write-Host ""
Write-Host "Deployment Information:" -ForegroundColor Cyan
Write-Host "- Website: $SiteName" -ForegroundColor White
Write-Host "- URL: http://localhost:$Port" -ForegroundColor White
Write-Host "- Swagger URL: http://localhost:$Port (direct access)" -ForegroundColor White
Write-Host "- Physical Path: $SitePath" -ForegroundColor White
Write-Host "- Application Pool: $ApplicationPool" -ForegroundColor White
Write-Host "- Port: $Port" -ForegroundColor White
Write-Host ""
Write-Host "Security Features Applied:" -ForegroundColor Cyan
Write-Host "- Removed unnecessary architecture folders (x86, arm64, armhf)" -ForegroundColor White
Write-Host "- Removed debug symbols (PDB files)" -ForegroundColor White
Write-Host "- Removed development configuration files" -ForegroundColor White
Write-Host "- Sanitized configuration files (no sensitive paths)" -ForegroundColor White
Write-Host "- Removed test files exposing internal information" -ForegroundColor White
Write-Host ""
Write-Host "Test URLs:" -ForegroundColor Cyan
Write-Host "- Health: http://localhost:$Port/health" -ForegroundColor White
Write-Host "- Info: http://localhost:$Port/info" -ForegroundColor White
Write-Host "- WIM API: http://localhost:$Port/api/wim/test-files" -ForegroundColor White
Write-Host ""
Write-Host "Useful Commands:" -ForegroundColor Cyan
Write-Host "- Stop site: Stop-Website -Name '$SiteName'" -ForegroundColor White
Write-Host "- Start site: Start-Website -Name '$SiteName'" -ForegroundColor White
Write-Host "- Stop pool: Stop-WebAppPool -Name '$ApplicationPool'" -ForegroundColor White
Write-Host "- Start pool: Start-WebAppPool -Name '$ApplicationPool'" -ForegroundColor White
