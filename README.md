# WimGetInfoApi - Wim Get Info Api 🎯

[![Production Ready](https://img.shields.io/badge/Status-Production%20Ready-brightgreen)](http://localhost:8080)
[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-purple)](https://dotnet.microsoft.com/)
[![Windows Auth](https://img.shields.io/badge/Auth-Windows-blue)](http://localhost:8080)

A **production-ready** ASP.NET Core 8 Web API for analyzing Windows Imaging Format (WIM) files with **Windows Authentication** and **SOLID architecture**.

## ✨ Key Features

- 🔍 **Complete WIM Analysis**: Extract detailed information from Windows Imaging files
- 🏗️ **SOLID Architecture**: Clean, maintainable code with dependency injection
- 🔐 **Windows Authentication**: Seamless browser-based authentication  
- 🚀 **IIS Ready**: Automated deployment with security hardening
- 📊 **Clean Swagger UI**: Optimized interface without cluttered schemas
- ⚡ **High Performance**: Transient services prevent memory issues

## � Quick Start

### Prerequisites
- .NET 8.0 SDK
- Windows 10+ or Windows Server 2016+
- IIS 10.0+ (for production with Windows Authentication)

### ⚡ Instant Setup
```powershell
# 1. Clone and build
git clone <repository-url>
cd WimGetInfoApi
# 📁 Note importante
Le fichier WIM à analyser doit être présent sur le serveur où l'API est exécutée. Indiquez le chemin complet du fichier WIM dans vos requêtes API.
dotnet restore && dotnet build

# 2. Deploy to IIS (recommended)
.\Deploy-ToIIS-Secured.ps1 -Force

# 3. Access API
# URL: http://localhost:8080
# Swagger: http://localhost:8080 (default homepage)
# Authentication: Automatic Windows Authentication
```

### 🎯 Quick Test
```powershell
# Test health endpoint
curl http://localhost:8080/health

# Test service info (requires Windows Auth)
curl http://localhost:8080/api/wim/service-info
```

## 📡 API Endpoints

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| `GET` | `/health` | API health status | ❌ No |
| `GET` | `/info` | API information | ❌ No |
| `GET` | `/api/wim/service-info` | Service diagnostics | ✅ Windows |
| `GET` | `/api/wim/image-info` | Specific WIM image analysis | ✅ Windows |
| `GET` | `/api/wim/all-images-info` | All images in WIM file | ✅ Windows |

### 💡 Usage Examples

#### PowerShell
```powershell
# Check API status
Invoke-RestMethod http://localhost:8080/health

# Analyze WIM image (Windows Auth automatic in browser)
$path = [System.Web.HttpUtility]::UrlEncode("C:\images\windows.wim")
$result = Invoke-RestMethod "http://localhost:8080/api/wim/image-info?wimFilePath=$path&imageIndex=1"
$result.data | Format-List
```

#### JavaScript
```javascript
// Using fetch API (Windows Authentication handled by browser)
fetch('http://localhost:8080/api/wim/service-info')
  .then(response => response.json())
  .then(data => console.log('Service Info:', data.data));
```

## 🏗️ Architecture

### Current Clean Architecture
```
┌──────────────────────────────────────────────────────────┐
│                 🌐 Presentation Layer                    │
│  • WimController (Simple REST endpoints)                │
│  • Clean Swagger UI (No schemas clutter)                │
│  • Windows Authentication (Browser-based)               │
└──────────────────────────────────────────────────────────┘
                              ↓
┌──────────────────────────────────────────────────────────┐
│                📋 Business Logic Layer                   │
│  • WimAnalyzerService (SOLID principles)                │
│  • Dependency injection throughout                      │
│  • Transient service lifetime (prevent caching issues)  │
└──────────────────────────────────────────────────────────┘
                              ↓
┌──────────────────────────────────────────────────────────┐
│                🔧 Infrastructure Layer                   │
│  • ManagedWimLib integration                            │
│  • File system access                                   │
│  • Native library initialization                        │
└──────────────────────────────────────────────────────────┘
```

### 🎯 SOLID Principles Implementation

| Principle | Implementation |
|-----------|----------------|
| **S**ingle Responsibility | Each service has one clear purpose |
| **O**pen/Closed | Services extensible via interfaces |
| **L**iskov Substitution | All interfaces properly implemented |
| **I**nterface Segregation | Small, focused interfaces |
| **D**ependency Inversion | Constructor injection throughout |

## � Security Features

### ✅ Current Security Implementation
- **Windows Authentication**: Microsoft.AspNetCore.Authentication.Negotiate
- **Input Validation**: Path sanitization and parameter validation
- **Security Headers**: Configured for production
- **File Access Control**: Restricted to valid WIM files
- **Build Security**: Debug symbols removed, unnecessary files cleaned

### 🛡️ Windows Authentication Flow
```
1. Browser requests protected endpoint
2. Server responds with 401 + WWW-Authenticate: Negotiate
3. Browser automatically sends Windows credentials
4. Server validates against Active Directory
5. User authenticated transparently
```

## � Response Format

All endpoints return consistent JSON responses:

```json
{
  "success": true,
  "message": "Operation completed successfully",
  "data": {
    // Endpoint-specific data
  },
  "errorDetails": null
}
```

### Example WIM Image Response
```json
{
  "success": true,
  "message": "Image information retrieved successfully",
  "data": {
    "index": 1,
    "name": "Windows 10 Pro",
    "architecture": "x64",
    "version": "10.0.19041",
    "edition": "Professional",
    "sizeMB": 15234,
    "files": 245678,
    "directories": 85432
  }
}
```

## � Deployment

### 🎯 Production IIS Deployment (Recommended)
```powershell
# One-command deployment with security hardening
.\Deploy-ToIIS-Secured.ps1 -Force

# Results:
# ✅ IIS features installed
# ✅ Windows Authentication enabled  
# ✅ Application deployed to port 8080
# ✅ Security hardening applied
# ✅ Ready for production use
```

### 🔧 Manual IIS Setup
```powershell
# Enable IIS features
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole, IIS-WindowsAuthentication

# Create app pool and site  
Import-Module WebAdministration
New-WebAppPool -Name "DemoGetInfoApiPool"
New-Website -Name "DemoGetInfoApi" -Port 8080 -PhysicalPath "C:\inetpub\wwwroot\DemoGetInfoApi"
```

### 📦 Build Configuration
- **Framework**: .NET 8.0
- **Runtime**: win-x64  
- **Mode**: Release with optimizations
- **Authentication**: Windows Authentication required
- **Port**: 8080 (configurable)

## 🛠️ Development

### 🏗️ Project Structure
```
DemoGetInfoApi/
├── Controllers/WimController.cs        # API endpoints  
├── Models/WimImageInfo.cs             # Response models
├── Services/WimAnalyzerService.cs     # Core service
├── Program.cs                         # App configuration
├── Deploy-ToIIS-Secured.ps1          # Deployment automation
└── UPDATED_TECHNICAL_DOCUMENTATION.md # Detailed docs
```

### 📚 Key Dependencies
```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.Negotiate" Version="8.0.10" />
<PackageReference Include="ManagedWimLib" Version="3.4.0" />
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.6.2" />
```

### 💻 Local Development
```powershell
# Development server (no Windows Auth - Kestrel limitation)
dotnet run --urls "http://localhost:5050"

# Production testing (Windows Auth works)  
.\Deploy-ToIIS-Secured.ps1 -Force
# Access: http://localhost:8080
```

## 🧪 Testing

### ✅ Health Check Test
```powershell
curl http://localhost:8080/health
# Expected: {"status":"Healthy","timestamp":"..."}
```

### 🔐 Windows Auth Test
```powershell
# Open browser to: http://localhost:8080/api/wim/service-info
# Browser will automatically authenticate with Windows credentials
```

### 📊 Service Test
```powershell
Invoke-RestMethod http://localhost:8080/api/wim/service-info
# Expected: Service configuration and status
```

## 🎯 Production Checklist

- ✅ **Windows Authentication**: Functional with browsers
- ✅ **SOLID Architecture**: Complete implementation  
- ✅ **Security Hardening**: Applied and tested
- ✅ **Clean Swagger UI**: No cluttered schemas
- ✅ **IIS Deployment**: Automated and reliable
- ✅ **Performance**: Optimized service lifetimes
- ✅ **Documentation**: Updated and accurate
- ✅ **Build Optimization**: Removed obsolete files

## 📋 API Testing Examples

### Browser Testing (Recommended)
Open these URLs in your browser (Windows Auth automatic):
```
http://localhost:8080                 # Swagger UI homepage
http://localhost:8080/health          # Health check
http://localhost:8080/info           # API information  
http://localhost:8080/api/wim/service-info  # Service diagnostics
```

### PowerShell Testing
```powershell
# Test with real WIM file
$wimPath = "C:\path\to\your\file.wim"  
$encodedPath = [System.Web.HttpUtility]::UrlEncode($wimPath)

# Get all images info
$response = Invoke-RestMethod "http://localhost:8080/api/wim/all-images-info?wimFilePath=$encodedPath"
$response.data | Select-Object index, name, sizeMB | Format-Table
```

## 📝 Recent Updates (Sept 2025)

### ✨ What's New
- **Simplified Authentication**: Basic Windows Auth that actually works
- **Clean Swagger Interface**: Removed schemas section for better UX  
- **Build Optimization**: Removed obsolete custom CSS/JS files
- **Updated Documentation**: Reflects current simplified architecture
- **Production Deployment**: Fully automated and tested

### �️ What Was Removed
- Complex ProducesResponseType attributes (caused schema bloat)
- Custom Swagger styling files (unnecessary complexity)
- Caching services (prevented fresh data)
- Complex authorization policies (caused authentication issues)

---

**🎉 Status**: Production Ready | **📅 Updated**: September 2025 | **🔗 URL**: http://localhost:8080
