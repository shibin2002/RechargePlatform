using Microsoft.OpenApi.Models;
using RechargeApi.Services;
using RechargePlatform.Common.Constants;
using RechargePlatform.Common.Logging;
using RechargePlatform.Common.Middleware;
using RechargePlatform.Data.Database;
using RechargePlatform.Data.Repositories;
using DotNetEnv;
using Serilog;

DotNetEnv.Env.TraversePath().Load();

SerilogConfiguration.ConfigureLogger("RechargeApi");

try
{
    Log.Information("Starting Main Telecom Recharge API...");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Telecom Recharge Platform API",
            Version = "v1",
            Description = "Production-style telecom prepaid recharge platform. 🔑 To test protected endpoints in Swagger UI, click the green 'Authorize 🔓' button at the top right and enter: pos_super_secret_api_key_2026"
        });

        // Add X-Api-Key Security Definition in Swagger
        c.AddSecurityDefinition(AuthConstants.ApiKeyHeaderName, new OpenApiSecurityScheme
        {
            Name = AuthConstants.ApiKeyHeaderName,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "ApiKeyScheme",
            In = ParameterLocation.Header,
            Description = "Enter API Key configured via RECHARGE_API_KEY environment variable"
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = AuthConstants.ApiKeyHeaderName
                    }
                },
                Array.Empty<string>()
            }
        });

        foreach (var file in Directory.GetFiles(AppContext.BaseDirectory, "*.xml"))
        {
            c.IncludeXmlComments(file);
        }
    });

    // Database and Repositories
    builder.Services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
    builder.Services.AddScoped<IRechargeRepository, RechargeRepository>();
    builder.Services.AddScoped<ICardRepository, CardRepository>();
    builder.Services.AddScoped<IAnalyticsRepository, AnalyticsRepository>();

    // HTTP Client for Provider with explicit 10s timeout
    var providerBaseUrl = Environment.GetEnvironmentVariable("PROVIDER_BASE_URL")
        ?? builder.Configuration["Provider:BaseUrl"]
        ?? "http://localhost:5005/";
    builder.Services.AddHttpClient<IProviderClient, ProviderClient>(client =>
    {
        client.BaseAddress = new Uri(providerBaseUrl);
        client.Timeout = TimeSpan.FromSeconds(10);
    });

    // Domain Services
    builder.Services.AddScoped<IRechargeService, RechargeService>();
    builder.Services.AddScoped<ICardImportService, CardImportService>();

    // Background Reconciliation Service
    builder.Services.AddHostedService<RechargeReconciliationBackgroundService>();

    // CORS for Frontend
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
    });

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    // Global exception handler to guarantee JSON error output
    app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Telecom Recharge API v1");
    });

    app.UseCors("AllowFrontend");

    // Custom API Key Authentication Middleware
    app.UseMiddleware<ApiKeyMiddleware>();

    app.MapControllers();

    app.MapGet("/", () => Results.Ok(new
    {
        Service = "Telecom Recharge API",
        Status = "Online",
        Port = 5000,
        Time = DateTime.UtcNow
    }));

    app.Run("http://localhost:5000");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Telecom Recharge API host terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}
