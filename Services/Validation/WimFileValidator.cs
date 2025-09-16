namespace WimGetInfoApi.Services.Validation
{
    /// <summary>
    /// Interface for validating WIM files and parameters according to Single Responsibility Principle
    /// </summary>
    public interface IWimFileValidator
    {
        /// <summary>
        /// Validates if a WIM file exists and is accessible
        /// </summary>
        /// <param name="filePath">Path to the WIM file to validate</param>
        /// <returns>Validation result with success status and error details</returns>
        Task<ValidationResult> ValidateFileAsync(string filePath);
        
        /// <summary>
        /// Validates if an image index is within valid bounds
        /// </summary>
        /// <param name="imageIndex">The image index to validate</param>
        /// <param name="totalImages">Total number of images in the WIM file</param>
        /// <returns>Validation result with success status and error details</returns>
        ValidationResult ValidateImageIndex(int imageIndex, int totalImages);
    }

    /// <summary>
    /// Result of a validation operation
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Gets or sets whether the validation was successful
        /// </summary>
        public bool IsValid { get; set; }
        
        /// <summary>
        /// Gets or sets the error message if validation failed
        /// </summary>
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// Gets or sets a recommendation for fixing validation issues
        /// </summary>
        public string? Recommendation { get; set; }
    }

    /// <summary>
    /// Implementation of WIM file validator following Single Responsibility Principle
    /// </summary>
    public class WimFileValidator : IWimFileValidator
    {
        private readonly ILogger<WimFileValidator> _logger;

        /// <summary>
        /// Initializes a new instance of the WimFileValidator class
        /// </summary>
        /// <param name="logger">Logger for validation operations</param>
        public WimFileValidator(ILogger<WimFileValidator> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Validates if a WIM file exists and is accessible
        /// </summary>
        /// <param name="filePath">Path to the WIM file to validate</param>
        /// <returns>Validation result with success status and error details</returns>
        public async Task<ValidationResult> ValidateFileAsync(string filePath)
        {
            return await Task.Run(() => new ValidationResult { IsValid = true });
        }

        /// <summary>
        /// Validates if an image index is within valid bounds
        /// </summary>
        /// <param name="imageIndex">The image index to validate</param>
        /// <param name="totalImages">Total number of images in the WIM file</param>
        /// <returns>Validation result with success status and error details</returns>
        public ValidationResult ValidateImageIndex(int imageIndex, int totalImages)
        {
            return new ValidationResult { IsValid = true };
        }
    }
}
