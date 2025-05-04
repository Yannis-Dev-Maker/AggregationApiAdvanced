using AgileActors.Models;
using AgileActors.Services;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// ✅ Clear existing providers and add debug + console logging
builder.Logging.ClearProviders();
builder.Logging.AddDebug();    // ➜ Logs to Output window (Visual Studio)
builder.Logging.AddConsole();  // ➜ Optional: logs to terminal
builder.Logging.SetMinimumLevel(LogLevel.Information); // Optional: filter logs

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Dynamically load API service definitions from Access DB
var apiServiceDefinitions = ApiServiceConfigLoader.LoadServices();

// Register all definitions as a singleton collection (optional reuse)
builder.Services.AddSingleton(apiServiceDefinitions);

// Register a factory to resolve all GenericApiService instances
builder.Services.AddScoped<IEnumerable<IAggregatorService>>(sp =>
{
    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
    return apiServiceDefinitions.Select(def =>
    {
        var logger = loggerFactory.CreateLogger<GenericApiService>();
        return new GenericApiService(def, logger);
    }).ToList();
});

// Add CORS policy for frontend and development
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
        else
        {
            policy.WithOrigins(
                    "https://www.pc-soft.gr",
                    "http://www.pc-soft.gr",
                    "https://pc-soft.gr",
                    "http://pc-soft.gr",
                    "https://localhost:5001",
                    "http://localhost:5000"
                )
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});

var app = builder.Build();

// Enable Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.ConfigObject.AdditionalItems["withCredentials"] = false;
    });
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthorization();
app.MapControllers();

app.Run();
