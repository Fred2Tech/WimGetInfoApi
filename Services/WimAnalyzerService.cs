using ManagedWimLib;
using WimGetInfoApi.Models;
using WimGetInfoApi.Services.Core;
using System.Collections.Generic;
using System.Threading;

namespace WimGetInfoApi.Services
{
    /// <summary>
    /// Interface for WIM file analysis services
    /// </summary>
    public interface IWimAnalyzerService
    {
        /// <summary>Get information about a specific WIM image</summary>
        Task<WimApiResponse<WimImageInfo>> GetWimImageInfoAsync(string wimFilePath, int imageIndex);
        /// <summary>Get information about all images in a WIM file</summary>
        Task<WimApiResponse<List<WimImageInfo>>> GetAllWimImagesInfoAsync(string wimFilePath);
    }

    /// <summary>
    /// WIM file analysis service using ManagedWimLib
    /// Follows SRP: single responsibility for WIM file analysis
    /// Follows DIP: depends on abstractions (interfaces)
    /// </summary>
    public class WimAnalyzerService : IWimAnalyzerService
    {
        private readonly ILogger<WimAnalyzerService> _logger;
        private readonly IWimLibraryInitializer _wimLibInitializer;
        private readonly Dictionary<string, object?> _propertyCache = new();
        private string? _lastWimFilePath = null;

        /// <summary>
        /// Initialize a new instance of the WIM analysis service
        /// DIP: Dependency injection via interfaces
        /// </summary>
        public WimAnalyzerService(ILogger<WimAnalyzerService> logger, IWimLibraryInitializer wimLibInitializer)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _wimLibInitializer = wimLibInitializer ?? throw new ArgumentNullException(nameof(wimLibInitializer));
        }

        /// <summary>
        /// SRP: Single responsibility for cache consistency management
        /// </summary>
        private void EnsureCacheConsistency(string wimFilePath)
        {
            // Check if we're switching to a different WIM file
            if (_lastWimFilePath != wimFilePath)
            {
                // Clear all caches when switching files to prevent contamination
                _propertyCache.Clear();
                
                // Force garbage collection to clear any native references
                GC.Collect();
                GC.WaitForPendingFinalizers();
                
                _logger.LogDebug("Switched to new WIM file, cache cleared: {WimFile}", wimFilePath);
            }
            else
            {
                // Always clear cache completely for every new request to ensure fresh data
                _propertyCache.Clear();
                _logger.LogDebug("Cache cleared for same WIM file: {WimFile}", wimFilePath);
            }
            
            _lastWimFilePath = wimFilePath;
        }

        /// <summary>
        /// Internal method to validate WIM file and get basic information
        /// SRP: Single responsibility for retrieving WIM file information
        /// </summary>
        private async Task<WimApiResponse<WimFileInfo>> GetWimFileInfoAsync(string wimFilePath)
        {
            // Security: Input parameter validation
            if (string.IsNullOrWhiteSpace(wimFilePath))
            {
                return new WimApiResponse<WimFileInfo>
                {
                    Success = false,
                    Message = "WIM file path cannot be empty"
                };
            }

            try
            {
                // Security: Path normalization to prevent traversal attacks
                wimFilePath = Path.GetFullPath(wimFilePath);
                
                // Ensure cache consistency when switching WIM files
                EnsureCacheConsistency(wimFilePath);
                
                // Ensure ManagedWimLib is initialized using singleton service
                if (!_wimLibInitializer.EnsureInitialized())
                {
                    return new WimApiResponse<WimFileInfo>
                    {
                        Success = false,
                        Message = "Unable to initialize ManagedWimLib. Check that libwim-15.dll is available.",
                        ErrorDetails = "ManagedWimLib initialization failed"
                    };
                }

                // Security: Architecture validation
                if (!Environment.Is64BitProcess)
                {
                    return new WimApiResponse<WimFileInfo>
                    {
                        Success = false,
                        Message = "This application requires an x64 platform"
                    };
                }

                // Security: File existence verification
                if (!File.Exists(wimFilePath))
                {
                    return new WimApiResponse<WimFileInfo>
                    {
                        Success = false,
                        Message = $"The WIM file does not exist: {wimFilePath}"
                    };
                }

                var extension = Path.GetExtension(wimFilePath).ToLowerInvariant();
                
                // Security: File extension validation
                if (extension != ".wim" && extension != ".esd")
                {
                    return new WimApiResponse<WimFileInfo>
                    {
                        Success = false,
                        Message = "Only .wim and .esd files are supported"
                    };
                }

                var fileInfo = new FileInfo(wimFilePath);
                
                using var wim = Wim.OpenWim(wimFilePath, OpenFlags.DEFAULT);
                var wimInfo = wim.GetWimInfo();
                
                var wimFileInfo = new WimFileInfo
                {
                    FilePath = wimFilePath,
                    FileName = fileInfo.Name,
                    FileSize = fileInfo.Length,
                    ImageCount = (int)wimInfo.ImageCount,
                    IsBootable = wimInfo.BootIndex > 0 ? "yes" : "no",
                    Created = fileInfo.CreationTime,
                    Modified = fileInfo.LastWriteTime
                };

                // Get basic information for all images
                for (int i = 1; i <= wimInfo.ImageCount; i++)
                {
                    var imageInfo = await GetBasicImageInfoAsync(wim, i);
                    if (imageInfo != null)
                    {
                        wimFileInfo.Images.Add(imageInfo);
                    }
                }

                return new WimApiResponse<WimFileInfo>
                {
                    Success = true,
                    Message = $"Information retrieved successfully for {wimInfo.ImageCount} image(s)",
                    Data = wimFileInfo
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing WIM file: {FilePath}", wimFilePath);
                return new WimApiResponse<WimFileInfo>
                {
                    Success = false,
                    Message = "Error analyzing WIM file",
                    ErrorDetails = ex.Message
                };
            }
        }

        /// <summary>
        /// Gets information for a specific WIM image
        /// </summary>
        public async Task<WimApiResponse<WimImageInfo>> GetWimImageInfoAsync(string wimFilePath, int imageIndex)
        {
            try
            {
                // Force complete cache clear at start of each request
                _propertyCache.Clear();
                
                // Ensure cache consistency when switching WIM files
                EnsureCacheConsistency(wimFilePath);
                
                // Ensure ManagedWimLib is initialized using singleton service
                if (!_wimLibInitializer.EnsureInitialized())
                {
                    return new WimApiResponse<WimImageInfo>
                    {
                        Success = false,
                        Message = "Unable to initialize ManagedWimLib. Check that libwim-15.dll is available.",
                        ErrorDetails = "ManagedWimLib initialization failed"
                    };
                }

                var fileInfoResponse = await GetWimFileInfoAsync(wimFilePath);
                if (!fileInfoResponse.Success || fileInfoResponse.Data == null)
                {
                    return new WimApiResponse<WimImageInfo>
                    {
                        Success = false,
                        Message = fileInfoResponse.Message,
                        ErrorDetails = fileInfoResponse.ErrorDetails
                    };
                }

                if (imageIndex < 1 || imageIndex > fileInfoResponse.Data.ImageCount)
                {
                    return new WimApiResponse<WimImageInfo>
                    {
                        Success = false,
                        Message = $"Index d''image invalide. Doit être entre 1 et {fileInfoResponse.Data.ImageCount}"
                    };
                }

                using var wim = Wim.OpenWim(wimFilePath, OpenFlags.DEFAULT);
                var detailedImageInfo = await GetDetailedImageInfoAsync(wim, wimFilePath, imageIndex);

                return new WimApiResponse<WimImageInfo>
                {
                    Success = true,
                    Message = $"Detailed information retrieved for image {imageIndex}",
                    Data = detailedImageInfo
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing WIM image: {FilePath}, Index: {Index}", wimFilePath, imageIndex);
                return new WimApiResponse<WimImageInfo>
                {
                    Success = false,
                    Message = "Error analyzing WIM image",
                    ErrorDetails = ex.Message
                };
            }
        }

        /// <summary>
        /// Gets information for all images in a WIM file
        /// </summary>
        public async Task<WimApiResponse<List<WimImageInfo>>> GetAllWimImagesInfoAsync(string wimFilePath)
        {
            try
            {
                // Ensure cache consistency when switching WIM files
                EnsureCacheConsistency(wimFilePath);
                
                // Ensure ManagedWimLib is initialized using singleton service
                if (!_wimLibInitializer.EnsureInitialized())
                {
                    return new WimApiResponse<List<WimImageInfo>>
                    {
                        Success = false,
                        Message = "Unable to initialize ManagedWimLib. Check that libwim-15.dll is available.",
                        ErrorDetails = "ManagedWimLib initialization failed"
                    };
                }

                var fileInfoResponse = await GetWimFileInfoAsync(wimFilePath);
                if (!fileInfoResponse.Success || fileInfoResponse.Data == null)
                {
                    return new WimApiResponse<List<WimImageInfo>>
                    {
                        Success = false,
                        Message = fileInfoResponse.Message,
                        ErrorDetails = fileInfoResponse.ErrorDetails
                    };
                }

                var allImages = new List<WimImageInfo>();
                using var wim = Wim.OpenWim(wimFilePath, OpenFlags.DEFAULT);
                
                for (int i = 1; i <= fileInfoResponse.Data.ImageCount; i++)
                {
                    var detailedImageInfo = await GetDetailedImageInfoAsync(wim, wimFilePath, i);
                    if (detailedImageInfo != null)
                    {
                        allImages.Add(detailedImageInfo);
                    }
                }

                return new WimApiResponse<List<WimImageInfo>>
                {
                    Success = true,
                    Message = $"Information retrieved for {allImages.Count} image(s)",
                    Data = allImages
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing all WIM images: {FilePath}", wimFilePath);
                return new WimApiResponse<List<WimImageInfo>>
                {
                    Success = false,
                    Message = "Error analyzing all WIM images",
                    ErrorDetails = ex.Message
                };
            }
        }

        private Task<WimImageInfo?> GetBasicImageInfoAsync(Wim wim, int imageIndex)
        {
            try
            {
                var imageInfo = new WimImageInfo
                {
                    Index = imageIndex,
                    Name = wim.GetImageName(imageIndex) ?? $"Image {imageIndex}",
                    Description = wim.GetImageDescription(imageIndex) ?? "",
                };
                return Task.FromResult<WimImageInfo?>(imageInfo);
            }
            catch
            {
                return Task.FromResult<WimImageInfo?>(null);
            }
        }

        private Task<WimImageInfo> GetDetailedImageInfoAsync(Wim wim, string wimFilePath, int imageIndex)
        {
            // Force complete cache clear for this request to ensure absolutely fresh data
            _propertyCache.Clear();
            
            var imageInfo = new WimImageInfo
            {
                Index = imageIndex,
                Name = wim.GetImageName(imageIndex) ?? $"Image {imageIndex}",
                Description = wim.GetImageDescription(imageIndex) ?? "",
                Architecture = GetArchitectureString(wim, wimFilePath, imageIndex),
                Hal = GetHalString(wim, wimFilePath, imageIndex),
                Version = GetVersionString(wim, wimFilePath, imageIndex),
                ServicePackBuild = GetServicePackBuildString(wim, wimFilePath, imageIndex),
                Installation = GetInstallationString(wim, wimFilePath, imageIndex),
                ProductType = GetProductTypeString(wim, wimFilePath, imageIndex),
                ProductSuite = GetProductSuiteString(wim, wimFilePath, imageIndex),
                SystemRoot = GetSystemRootString(wim, wimFilePath, imageIndex),
                Edition = GetEditionString(wim, wimFilePath, imageIndex),
                Languages = GetLanguagesString(wim, wimFilePath, imageIndex)
            };

            // Size handling
            var sizeStr = GetImageProperty(wim, wimFilePath, imageIndex, "TOTALBYTES");
            if (long.TryParse(sizeStr, out long sizeBytes))
            {
                imageInfo.SizeBytes = sizeBytes;
                imageInfo.SizeMB = sizeBytes / (1024 * 1024);
            }

            // Service Pack Level
            var spLevelStr = GetImageProperty(wim, wimFilePath, imageIndex, "SERVICEPACK_LEVEL");
            if (int.TryParse(spLevelStr, out int spLevel))
            {
                imageInfo.ServicePackLevel = spLevel;
            }

            // Directory and file count
            var dirCountStr = GetImageProperty(wim, wimFilePath, imageIndex, "DIRCOUNT");
            if (long.TryParse(dirCountStr, out long dirCount))
            {
                imageInfo.Directories = dirCount;
            }

            var fileCountStr = GetImageProperty(wim, wimFilePath, imageIndex, "FILECOUNT");
            if (long.TryParse(fileCountStr, out long fileCount))
            {
                imageInfo.Files = fileCount;
            }

            // Creation and modification dates - same logic as reference code
            imageInfo.Created = GetFormattedDateTime(wim, wimFilePath, imageIndex, "CREATIONTIME");
            imageInfo.Modified = GetFormattedDateTime(wim, wimFilePath, imageIndex, "LASTMODIFICATIONTIME");

            // Bootable image - multi-path like reference program
            string bootInfo = GetImageProperty(wim, wimFilePath, imageIndex, "WINDOWS/BOOTABLE") ?? 
                             GetImageProperty(wim, wimFilePath, imageIndex, "BOOTABLE") ??
                             GetImageProperty(wim, wimFilePath, imageIndex, "WIM_BOOTABLE");
            
            imageInfo.IsBootable = !string.IsNullOrEmpty(bootInfo) && bootInfo.Equals("Yes", StringComparison.OrdinalIgnoreCase) ? "yes" : "no";

            return Task.FromResult(imageInfo);
        }

        private string GetHalString(Wim wim, string wimFilePath, int imageIndex)
        {
            // HAL - use extended Windows path (same logic as reference code)
            string? hal = GetImageProperty(wim, wimFilePath, imageIndex, "WINDOWS/HAL") ?? 
                         GetImageProperty(wim, wimFilePath, imageIndex, "HAL") ?? 
                         GetImageProperty(wim, wimFilePath, imageIndex, "HALSYSTEM");
            
            return hal ?? "Not specified";
        }

        private string GetArchitectureString(Wim wim, string wimFilePath, int imageIndex)
        {
            // Architecture - use extended Windows paths (same logic as reference code)
            string? architecture = GetImageProperty(wim, wimFilePath, imageIndex, "WINDOWS/ARCH") ?? 
                                  GetImageProperty(wim, wimFilePath, imageIndex, "ARCHITECTURE") ??
                                  GetImageProperty(wim, wimFilePath, imageIndex, "PROCESSORARCHITECTURE");
            
            _logger.LogDebug("Raw architecture value: {Architecture}", architecture);
            var result = GetArchitectureName(architecture);
            _logger.LogDebug("Mapped architecture value: {Result}", result);
            return result;
        }

        private static readonly Dictionary<string, string> ArchitectureMap = new Dictionary<string, string>
        {
            {"0", "x86"},
            {"9", "x64"},
            {"12", "ARM64"}
        };

        private static string GetArchitectureName(string? archStr)
        {
            if (string.IsNullOrWhiteSpace(archStr))
                return "Not specified";

            return ArchitectureMap.TryGetValue(archStr, out string? architecture) ? architecture : archStr;
        }

        private string GetVersionString(Wim wim, string wimFilePath, int imageIndex)
        {
            // Version - use extended Windows paths with all variants (same logic as reference code)
            string? version = GetImageProperty(wim, wimFilePath, imageIndex, "WINDOWS/VERSION") ?? 
                             GetImageProperty(wim, wimFilePath, imageIndex, "WINDOWS/PRODUCTVERSION") ??
                             GetImageProperty(wim, wimFilePath, imageIndex, "WINDOWS/DISPLAYVERSION") ??
                             GetImageProperty(wim, wimFilePath, imageIndex, "VERSION") ??
                             GetImageProperty(wim, wimFilePath, imageIndex, "DISPLAYVERSION") ??
                             GetImageProperty(wim, wimFilePath, imageIndex, "PRODUCTVERSION");
            
            // Try to build version from components (as in reference code)
            if (string.IsNullOrEmpty(version))
            {
                string? major = GetImageProperty(wim, wimFilePath, imageIndex, "WINDOWS/VERSION/MAJOR");
                string? minor = GetImageProperty(wim, wimFilePath, imageIndex, "WINDOWS/VERSION/MINOR");
                string? build = GetImageProperty(wim, wimFilePath, imageIndex, "WINDOWS/VERSION/BUILD");
                
                if (!string.IsNullOrEmpty(major) && !string.IsNullOrEmpty(minor))
                {
                    version = $"{major}.{minor}";
                    if (!string.IsNullOrEmpty(build))
                    {
                        version += $".{build}";
                    }
                }
            }
            
            return version ?? "Not specified";
        }

        private string GetServicePackBuildString(Wim wim, string wimFilePath, int imageIndex)
        {
            // Service Pack Build - multi-path like reference program
            string? spBuild = GetImageProperty(wim, wimFilePath, imageIndex, "WINDOWS/SERVICEPACK/BUILD") ?? 
                             GetImageProperty(wim, wimFilePath, imageIndex, "WINDOWS/VERSION/SPBUILD") ??
                             GetImageProperty(wim, wimFilePath, imageIndex, "SERVICEBUILD") ?? 
                             GetImageProperty(wim, wimFilePath, imageIndex, "SERVICEPACKBUILD") ?? 
                             GetImageProperty(wim, wimFilePath, imageIndex, "BUILD");
            
            return spBuild ?? "";
        }

        private string GetInstallationString(Wim wim, string wimFilePath, int imageIndex)
        {
            // Installation type - multi-path like reference program
            string? installation = GetImageProperty(wim, wimFilePath, imageIndex, "WINDOWS/INSTALLATIONTYPE") ??
                                  GetImageProperty(wim, wimFilePath, imageIndex, "INSTALLATIONTYPE") ?? 
                                  GetImageProperty(wim, wimFilePath, imageIndex, "INSTALLATION") ?? 
                                  GetImageProperty(wim, wimFilePath, imageIndex, "IMAGETYPE");
            
            return installation ?? "";
        }

        private string GetProductTypeString(Wim wim, string wimFilePath, int imageIndex)
        {
            // Product type - multi-path like reference program
            string? productType = GetImageProperty(wim, wimFilePath, imageIndex, "WINDOWS/PRODUCTTYPE") ??
                                 GetImageProperty(wim, wimFilePath, imageIndex, "PRODUCTTYPE") ?? 
                                 GetImageProperty(wim, wimFilePath, imageIndex, "PRODUCT_TYPE");
            
            return productType ?? "";
        }

        private string GetProductSuiteString(Wim wim, string wimFilePath, int imageIndex)
        {
            // Product suite - multi-path like reference program
            string? productSuite = GetImageProperty(wim, wimFilePath, imageIndex, "WINDOWS/PRODUCTSUITE") ??
                                  GetImageProperty(wim, wimFilePath, imageIndex, "PRODUCTSUITE") ?? 
                                  GetImageProperty(wim, wimFilePath, imageIndex, "PRODUCT_SUITE");
            
            return productSuite ?? "";
        }

        private string GetSystemRootString(Wim wim, string wimFilePath, int imageIndex)
        {
            // System root - multi-path like reference program
            string? systemRoot = GetImageProperty(wim, wimFilePath, imageIndex, "WINDOWS/SYSTEMROOT") ??
                                GetImageProperty(wim, wimFilePath, imageIndex, "SYSTEMROOT") ?? 
                                GetImageProperty(wim, wimFilePath, imageIndex, "WINDOWS") ?? 
                                GetImageProperty(wim, wimFilePath, imageIndex, "WINDIR");
            
            return systemRoot ?? "";
        }

        private string GetEditionString(Wim wim, string wimFilePath, int imageIndex)
        {
            // Edition - multi-path like reference program
            string? edition = GetImageProperty(wim, wimFilePath, imageIndex, "WINDOWS/EDITION") ??
                             GetImageProperty(wim, wimFilePath, imageIndex, "WINDOWS/EDITIONID") ??
                             GetImageProperty(wim, wimFilePath, imageIndex, "WINDOWS/DISPLAYNAME") ??
                             GetImageProperty(wim, wimFilePath, imageIndex, "EDITION") ?? 
                             GetImageProperty(wim, wimFilePath, imageIndex, "EDITIONID") ?? 
                             GetImageProperty(wim, wimFilePath, imageIndex, "PRODUCTNAME") ??
                             GetImageProperty(wim, wimFilePath, imageIndex, "DISPLAYNAME");
            
            return edition ?? "";
        }

        private string GetLanguagesString(Wim wim, string wimFilePath, int imageIndex)
        {
            // Languages - multi-path like reference program
            string? languages = GetImageProperty(wim, wimFilePath, imageIndex, "WINDOWS/LANGUAGES") ?? 
                               GetImageProperty(wim, wimFilePath, imageIndex, "WINDOWS/LANGUAGES/DEFAULT") ??
                               GetImageProperty(wim, wimFilePath, imageIndex, "LANGUAGES") ??
                               GetImageProperty(wim, wimFilePath, imageIndex, "DEFAULT_LANGUAGE");
            
            return languages ?? "";
        }

        private string? GetImageProperty(Wim wim, string wimFilePath, int imageIndex, string propertyName)
        {
            // Use the wim object passed as parameter to avoid additional I/O
            try
            {
                var value = wim.GetImageProperty(imageIndex, propertyName);
                
                // Add detailed logging for troubleshooting
                _logger.LogDebug("Property read: WIM={WimPath}, Index={Index}, Property={Property}, Value={Value}", 
                    wimFilePath, imageIndex, propertyName, value ?? "null");
                    
                return value;
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Property read failed: WIM={WimPath}, Index={Index}, Property={Property}, Error={Error}", 
                    wimFilePath, imageIndex, propertyName, ex.Message);
                return null;
            }
        }

        private DateTime? GetFormattedDateTime(Wim wim, string wimFilePath, int imageIndex, string dateProperty)
        {
            // First try direct property
            string? dateTimeStr = GetImageProperty(wim, wimFilePath, imageIndex, dateProperty);
            
            // If not found, try HIGHPART and LOWPART components (as in reference code)
            if (string.IsNullOrEmpty(dateTimeStr))
            {
                string? highPart = GetImageProperty(wim, wimFilePath, imageIndex, $"{dateProperty}/HIGHPART");
                string? lowPart = GetImageProperty(wim, wimFilePath, imageIndex, $"{dateProperty}/LOWPART");
                if (!string.IsNullOrEmpty(highPart) && !string.IsNullOrEmpty(lowPart))
                {
                    dateTimeStr = $"{highPart}:{lowPart}";
                }
            }

            return FormatDateTime(dateTimeStr);
        }

        private DateTime? FormatDateTime(string? dateTimeStr)
        {
            if (string.IsNullOrWhiteSpace(dateTimeStr) || dateTimeStr == "Not specified")
                return null;

            // If it's a Windows FILETIME with high and low parts (e.g., 0x01DC08B6:0x1A436C39)
            if (dateTimeStr.Contains(":") && dateTimeStr.Contains("0x"))
            {
                try
                {
                    var parts = dateTimeStr.Split(':');
                    if (parts.Length == 2)
                    {
                        // Extract and validate hexadecimal parts
                        string highPartStr = parts[0].Replace("0x", "").Trim();
                        string lowPartStr = parts[1].Replace("0x", "").Trim();
                        
                        // Length validation to avoid overflow
                        if (highPartStr.Length <= 8 && lowPartStr.Length <= 8 &&
                            uint.TryParse(highPartStr, System.Globalization.NumberStyles.HexNumber, null, out uint highPart) &&
                            uint.TryParse(lowPartStr, System.Globalization.NumberStyles.HexNumber, null, out uint lowPart))
                        {
                            // Reconstruct FILETIME (64-bit)
                            long fileTimeValue = ((long)highPart << 32) | lowPart;
                            
                            // FILETIME range validation (between 1601 and 3000)
                            if (fileTimeValue > 0 && fileTimeValue < 0x7FFFFFFFFFFFFFFF)
                            {
                                DateTime convertedDateTime = DateTime.FromFileTime(fileTimeValue);
                                // Year validation to avoid aberrant dates
                                if (convertedDateTime.Year >= 1990 && convertedDateTime.Year <= 2050)
                                {
                                    return convertedDateTime;
                                }
                            }
                        }
                    }
                }
                catch (ArgumentOutOfRangeException)
                {
                    return null;
                }
                catch (Exception)
                {
                    return null;
                }
            }

            // Try to parse different date formats with validation
            if (DateTime.TryParse(dateTimeStr, out DateTime parsedDateTime))
            {
                // Year validation
                if (parsedDateTime.Year >= 1990 && parsedDateTime.Year <= 2050)
                {
                    return parsedDateTime;
                }
            }

            // If it's a Windows timestamp (FILETIME) with validation
            if (long.TryParse(dateTimeStr, out long fileTime) && fileTime > 0 && fileTime < 0x7FFFFFFFFFFFFFFF)
            {
                try
                {
                    DateTime dt = DateTime.FromFileTime(fileTime);
                    if (dt.Year >= 1990 && dt.Year <= 2050)
                    {
                        return dt;
                    }
                }
                catch (ArgumentOutOfRangeException)
                {
                    return null;
                }
                catch (Exception)
                {
                    return null;
                }
            }

            return null;
        }
    }
}
