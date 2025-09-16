using ManagedWimLib;

namespace WimGetInfoApi.Services.Core
{
    /// <summary>
    /// Singleton service to manage ManagedWimLib initialization
    /// </summary>
    public interface IWimLibraryInitializer
    {
        /// <summary>
        /// Ensures ManagedWimLib is initialized and returns success status
        /// </summary>
        bool EnsureInitialized();
        
        /// <summary>
        /// Gets whether the library is currently initialized
        /// </summary>
        bool IsInitialized { get; }
    }

    /// <summary>
    /// Singleton implementation of WIM library initializer
    /// </summary>
    public class WimLibraryInitializer : IWimLibraryInitializer
    {
        private readonly ILogger<WimLibraryInitializer> _logger;
        private readonly object _lockObject = new object();
        private volatile bool _isInitialized = false;
        private volatile bool _initializationFailed = false;

        public WimLibraryInitializer(ILogger<WimLibraryInitializer> logger)
        {
            _logger = logger;
        }

        public bool IsInitialized => _isInitialized;

        public bool EnsureInitialized()
        {
            if (_isInitialized) return true;
            if (_initializationFailed) return false;

            lock (_lockObject)
            {
                if (_isInitialized) return true;
                if (_initializationFailed) return false;

                try
                {
                    var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    
                    // Try multiple search paths for the library
                    var searchPaths = new[]
                    {
                        Path.Combine(baseDir, "x64", "libwim-15.dll"),
                        Path.Combine(baseDir, "publish", "x64", "libwim-15.dll"),
                        Path.Combine(baseDir, "win-x64", "x64", "libwim-15.dll"),
                        Path.Combine(baseDir, "libwim-15.dll"),
                        Path.Combine(baseDir, "runtimes", "win-x64", "native", "libwim-15.dll")
                    };
                    
                    string? libPath = searchPaths.FirstOrDefault(File.Exists);
                    
                    if (!string.IsNullOrEmpty(libPath))
                    {
                        _logger.LogDebug("Attempting to initialize ManagedWimLib with library at: {LibPath}", libPath);
                        Wim.GlobalInit(libPath);
                        _isInitialized = true;
                        _logger.LogInformation("ManagedWimLib initialized successfully: {LibPath}", libPath);
                    }
                    else
                    {
                        // Try default initialization - this will search PATH and system directories
                        _logger.LogDebug("No library found in search paths, trying default initialization");
                        Wim.GlobalInit();
                        _isInitialized = true;
                        _logger.LogInformation("ManagedWimLib initialized with default library search");
                    }
                    
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to initialize ManagedWimLib");
                    _initializationFailed = true;
                    return false;
                }
            }
        }
    }
}
