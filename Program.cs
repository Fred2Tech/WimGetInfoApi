using WimGetInfoApi.Services;
using WimGetInfoApi.Services.Configuration;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.Negotiate;
using ManagedWimLib;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Windows Authentication - Simple configuration
builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
    .AddNegotiate();

// Basic authorization
builder.Services.AddAuthorization();

// Swagger configuration
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Wim Get Info Api",
        Version = "v1",
        Description = "API for analyzing Windows Imaging (WIM) files with Windows Authentication."
    });
});

// Register WIM services using SOLID extension (includes all dependencies)
builder.Services.ConfigureWimServices(builder.Configuration, builder.Environment);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Wim Get Info Api v1");
    c.RoutePrefix = string.Empty; // Swagger UI accessible at root URL
});

// Authentication order matters!
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health endpoint (anonymous access for monitoring)
app.MapGet("/health", () => 
{
    var port = Environment.GetEnvironmentVariable("ASPNETCORE_PORT") ??
        (builder.Configuration["ASPNETCORE_URLS"]?.Split(':').Last() ?? "");
    return Results.Ok(new 
    { 
        Status = "Healthy",
        Timestamp = DateTime.UtcNow,
        Version = "1.0.0",
        Platform = Environment.Is64BitProcess ? "x64" : "x86",
        DotNetVersion = Environment.Version.ToString(),
        Port = port
    });
})
.WithName("HealthCheck")
.WithTags("Health");

// API information endpoint (anonymous access for basic info)
app.MapGet("/info", () => 
{
    var port = Environment.GetEnvironmentVariable("ASPNETCORE_PORT") ??
        (builder.Configuration["ASPNETCORE_URLS"]?.Split(':').Last() ?? "");
    return Results.Ok(new 
    { 
        Name = "WimGetInfoApi - Wim Get Info Api",
        Description = "API for analyzing WIM (Windows Imaging Format) files with Windows Authentication",
        Version = "1.0.0",
        Authentication = "Windows Authentication Available",
        Port = port,
        Endpoints = new[]
        {
            "/health - API health check",
            "/info - API information",
            "/api/wim/service-info - Service diagnostics",
            "/api/wim/image-info - Specific image information",
            "/api/wim/all-images-info - All images information"
        },
        Documentation = "Available at root URL (Swagger UI)"
    });
})
.WithName("ApiInfo")
.WithTags("Information");

// Initialize ManagedWimLib
try
{
    Wim.GlobalInit();
    Console.WriteLine("✓ ManagedWimLib initialized successfully");
}
catch (Exception ex)
{
    Console.WriteLine($"✗ Failed to initialize ManagedWimLib: {ex.Message}");
}

// Cleanup handler
AppDomain.CurrentDomain.ProcessExit += (sender, args) =>
{
    try
    {
        Wim.GlobalCleanup();
        Console.WriteLine("✓ ManagedWimLib cleanup completed");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"✗ ManagedWimLib cleanup error: {ex.Message}");
    }
};

app.Run();
