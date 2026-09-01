using MockProviderApi.Services;
using RechargePlatform.Common.Logging;
using Serilog;

SerilogConfiguration.ConfigureLogger("MockProviderApi");

try
{
    Log.Information("Starting Mock Telecom Provider API...");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "Mock Telecom Provider API", Version = "v1" });
    });

    builder.Services.AddSingleton<IProviderStateStore, ProviderStateStore>();

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        });
    });

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseCors("AllowAll");

    app.MapControllers();

    var port = Environment.GetEnvironmentVariable("PORT") ?? "5005";
    var portNumber = int.TryParse(port, out var parsedPort) ? parsedPort : 5005;

    app.MapGet("/", () => Results.Ok(new
    {
        Service = "Mock Telecom Provider API",
        Status = "Online",
        Port = portNumber,
        Time = DateTime.UtcNow
    }));

    app.Run($"http://0.0.0.0:{port}");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Mock Telecom Provider API terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}
