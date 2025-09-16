using WimGetInfoApi.Services;
using WimGetInfoApi.Services.Validation;
using WimGetInfoApi.Services.Caching;
using WimGetInfoApi.Services.Core;

namespace WimGetInfoApi.Services.Configuration
{
    /// <summary>
    /// Extension methods for configuring SOLID-compliant services
    /// DIP: Configuration depends on abstractions
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Register all WIM analysis services following SOLID principles
        /// </summary>
        public static IServiceCollection AddWimAnalysisServices(this IServiceCollection services)
        {
            // Infrastructure services (SRP)
            services.AddSingleton<IWimLibraryInitializer, WimLibraryInitializer>();
            
            // Validation services (SRP)
            services.AddTransient<IWimFileValidator, WimFileValidator>();
            
            // Caching services (SRP) - DISABLED to prevent cross-contamination
            // services.AddSingleton<IWimPropertyCache, WimPropertyCache>();
            
            // Core reading services (SRP + ISP) - Changed to Transient to prevent caching issues
            services.AddTransient<IWimFileReader, WimFileReader>();
            services.AddTransient<IWimImageReader, WimImageReader>();
            services.AddTransient<IWimPropertyExtractor, WimPropertyExtractor>();
            
            // Main service (DIP - depends on abstractions) - TRANSIENT to force fresh instances
            services.AddTransient<IWimAnalyzerService, WimAnalyzerService>();
            
            return services;
        }

        /// <summary>
        /// Configure WIM analysis services - ONLY REAL SERVICE, NO MOCK WITH FAKE DATA
        /// OCP: Open for extension with new service configurations
        /// </summary>
        public static IServiceCollection ConfigureWimServices(this IServiceCollection services, 
            IConfiguration configuration, 
            IWebHostEnvironment environment)
        {
            var useRealService = configuration.GetValue<bool>("WimAnalyzer:UseRealService", true);
            var enableManagedWimLib = configuration.GetValue<bool>("WimAnalyzer:EnableManagedWimLib", true);

            // ONLY use real service - NO MOCK with fake hardcoded data
            if (useRealService && enableManagedWimLib)
            {
                services.AddWimAnalysisServices();
                Console.WriteLine("✓ Real WIM analysis services registered (SOLID architecture)");
            }
            else
            {
                throw new InvalidOperationException("Real WIM service is required. Mock service with fake data has been removed.");
            }

            return services;
        }
    }
}
