using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using WimGetInfoApi.Models;
using WimGetInfoApi.Services;
using System.ComponentModel.DataAnnotations;

namespace WimGetInfoApi.Controllers
{
    /// <summary>
    /// Controller for analyzing WIM (Windows Imaging Format) files
    /// Follows SRP principle: single responsibility for HTTP routing
    /// Requires Windows Authentication for access
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class WimController : ControllerBase
    {
        private readonly IWimAnalyzerService _wimAnalyzerService;
        private readonly ILogger<WimController> _logger;

        /// <summary>
        /// Initializes a new instance of the WIM controller
        /// DIP: Depends on abstractions, not concrete implementations
        /// </summary>
        public WimController(IWimAnalyzerService wimAnalyzerService, ILogger<WimController> logger)
        {
            _wimAnalyzerService = wimAnalyzerService ?? throw new ArgumentNullException(nameof(wimAnalyzerService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets service type information for diagnostics
        /// </summary>
        /// <returns>Service information</returns>
        [HttpGet("service-info")]
        public ActionResult<object> GetServiceInfo()
        {
            var serviceType = _wimAnalyzerService.GetType().Name;
            var isSOLID = serviceType.Contains("SOLID");
            
            return Ok(new
            {
                ServiceType = serviceType,
                IsSOLIDService = isSOLID,
                Timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Debug endpoint to view raw WIM XML data
        /// </summary>
        /// <param name="wimFilePath">Path to the WIM file</param>
        /// <returns>Raw XML data from WIM file</returns>
        [HttpGet("debug-xml")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public ActionResult<object> GetWimXmlDebug([Required] string wimFilePath)
        {
            // Security: Input parameter validation
            if (string.IsNullOrWhiteSpace(wimFilePath))
            {
                return BadRequest(new { Error = "WIM file path cannot be empty" });
            }

            // Security: Path normalization
            try
            {
                wimFilePath = Path.GetFullPath(wimFilePath);
            }
            catch (Exception)
            {
                return BadRequest(new { Error = "Invalid file path" });
            }

            try
            {
                using var wim = ManagedWimLib.Wim.OpenWim(wimFilePath, ManagedWimLib.OpenFlags.DEFAULT);
                var xmlData = wim.GetXmlData();
                
                return Ok(new
                {
                    FilePath = wimFilePath,
                    XmlDataLength = xmlData?.Length ?? 0,
                    HasXmlData = !string.IsNullOrEmpty(xmlData),
                    XmlPreview = xmlData?.Length > 1000 ? xmlData.Substring(0, 1000) + "..." : xmlData,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading WIM file XML: {FilePath}", wimFilePath);
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Get detailed information about a specific WIM image
        /// </summary>
        /// <param name="wimFilePath">Path to the WIM file</param>
        /// <param name="imageIndex">Image index (1, 2, 3, etc.)</param>
        /// <returns>Detailed WIM image information</returns>
        [HttpGet("image-info")]
        public async Task<ActionResult> GetWimImageInfo(
            [FromQuery, Required] string wimFilePath,
            [FromQuery, Required] int imageIndex)
        {
            // Security: Input parameter validation
            if (string.IsNullOrWhiteSpace(wimFilePath))
            {
                return BadRequest(new WimApiResponse<WimImageInfo>
                {
                    Success = false,
                    Message = "WIM file path cannot be empty"
                });
            }

            if (imageIndex <= 0)
            {
                return BadRequest(new WimApiResponse<WimImageInfo>
                {
                    Success = false,
                    Message = "Image index must be greater than 0"
                });
            }

            // Security: Path normalization
            try
            {
                wimFilePath = Path.GetFullPath(wimFilePath);
            }
            catch (Exception)
            {
                return BadRequest(new WimApiResponse<WimImageInfo>
                {
                    Success = false,
                    Message = "Invalid file path"
                });
            }

            _logger.LogInformation("Request for image {Index} information from WIM file: {FilePath}", 
                imageIndex, wimFilePath);

            var result = await _wimAnalyzerService.GetWimImageInfoAsync(wimFilePath, imageIndex);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Get detailed information about all images in a WIM file
        /// </summary>
        /// <param name="wimFilePath">Path to the WIM file</param>
        /// <returns>List of detailed information for all images</returns>
        [HttpGet("all-images-info")]
        public async Task<ActionResult> GetAllWimImagesInfo(
            [FromQuery, Required] string wimFilePath)
        {
            // Security: Input parameter validation
            if (string.IsNullOrWhiteSpace(wimFilePath))
            {
                return BadRequest(new WimApiResponse<List<WimImageInfo>>
                {
                    Success = false,
                    Message = "WIM file path cannot be empty"
                });
            }

            // Security: Path normalization
            try
            {
                wimFilePath = Path.GetFullPath(wimFilePath);
            }
            catch (Exception)
            {
                return BadRequest(new WimApiResponse<List<WimImageInfo>>
                {
                    Success = false,
                    Message = "Invalid file path"
                });
            }

            _logger.LogInformation("Request for all images information from WIM file: {FilePath}", wimFilePath);

            var result = await _wimAnalyzerService.GetAllWimImagesInfoAsync(wimFilePath);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Analyze a WIM file with advanced parameters
        /// <summary>
        /// Analyze a WIM file with advanced parameters
        /// </summary>
        /// <param name="request">WIM analysis request</param>
        /// <returns>Analysis results</returns>
        [HttpPost("analyze")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public ActionResult AnalyzeWim([FromBody] dynamic request)
        {
            _logger.LogInformation("Advanced WIM file analysis request received");

            try
            {
                // Cette méthode est cachée de Swagger (IgnoreApi = true)
                return Ok(new { 
                    Success = true, 
                    Message = "Analysis endpoint hidden from Swagger",
                    Data = "Use /api/wim/image-info or /api/wim/all-images-info instead" 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during advanced WIM file analysis");
                return StatusCode(500, new 
                {
                    Success = false,
                    Message = "Internal server error during analysis",
                    ErrorDetails = ex.Message
                });
            }
        }

        /// <summary>
        /// Check if a WIM file exists and is accessible
        /// </summary>
        /// <param name="wimFilePath">Path to the WIM file</param>
        /// <returns>File availability status</returns>
        [HttpGet("check-file")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public ActionResult CheckWimFile([FromQuery, Required] string wimFilePath)
        {
            // Security: Input parameter validation
            if (string.IsNullOrWhiteSpace(wimFilePath))
            {
                return Ok(new
                {
                    Success = false,
                    Message = "WIM file path cannot be empty"
                });
            }

            try
            {
                var normalizedPath = Path.GetFullPath(wimFilePath);
                var exists = System.IO.File.Exists(normalizedPath);
                var fileInfo = exists ? new FileInfo(normalizedPath) : null;

                return Ok(new
                {
                    Success = exists,
                    Message = exists ? "File accessible" : "File not found",
                    Data = exists ? new
                    {
                        Path = normalizedPath,
                        Size = fileInfo!.Length,
                        Created = fileInfo.CreationTime,
                        Modified = fileInfo.LastWriteTime,
                        Extension = fileInfo.Extension
                    } : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking file: {FilePath}", wimFilePath);
                return Ok(new
                {
                    Success = false,
                    Message = "Error checking file",
                    ErrorDetails = ex.Message
                });
            }
        }
    }
}
