using Zayun.Persistence.Seeding;
using Zayun.Presentation;
using Zayun.Presentation.Extensions;
using Zayun.Presentation.Settings.Authentication;
using Zayun.Presentation.Settings.Cors;
using Zayun.Presentation.Settings.OpenApi;
using Zayun.Worker;
using Serilog;

// A plain-text file next to the app that records each start-up step. On shared hosting (SmarterASP) it's often the
// only way to see why the API didn't start, because the logger itself may not be running yet.
var diagnosticDirectory = Path.Combine(AppContext.BaseDirectory, "App_Data");
var diagnosticFile = Path.Combine(diagnosticDirectory, "startup-diagnostics.log");

void WriteStartupDiagnostic(string message, Exception? exception = null)
{
    try
    {
        Directory.CreateDirectory(diagnosticDirectory);
        var entry = $"[{DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss.fff zzz}] {message}{Environment.NewLine}";
        if (exception is not null) entry += exception + Environment.NewLine;
        File.AppendAllText(diagnosticFile, entry);
    }
    catch
    {
        // Diagnostic logging must never prevent the API from starting.
    }
}

// Bootstrap logger for failures before application logging is configured.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    WriteStartupDiagnostic("Zayun API startup initiated.");

    var builder = WebApplication.CreateBuilder(args);
    WriteStartupDiagnostic($"Environment: {builder.Environment.EnvironmentName}");
    WriteStartupDiagnostic($"Content root: {builder.Environment.ContentRootPath}");

    // Serilog, from the "Serilog" section of appsettings.json
    builder.Services.AddSerilog((services, logger) => logger
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services));

    // Every layer (one composition root)
    builder.Services
        .AddPresentation(builder.Configuration)
        .AddWorker(builder.Configuration);

    var app = builder.Build();
    WriteStartupDiagnostic("Application built.");

    // Migrations, roles, first admin, starter content
    await app.Services.SeedDatabaseAsync();
    WriteStartupDiagnostic("Database initialization completed.");

    // HTTP pipeline
    app.UseSerilogRequestLogging();
    app.UseExceptionHandler();
    if (!app.Environment.IsDevelopment())
        app.UseHttpsRedirection();
    app.UseZayunStaticFiles();       // /uploads/… (pictures)
    app.UseZayunOpenApi();           // /swagger (when OpenApi:Enabled)
    app.UseZayunCors();              // before authentication: browser preflights need no sign-in
    app.UseZayunAuthentication();
    app.UseRateLimiter();            // after authentication; per-endpoint policies (Contact, sign-in)

    app.MapZayunEndpoints();

    app.Lifetime.ApplicationStarted.Register(() => WriteStartupDiagnostic("Zayun API started successfully."));
    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    WriteStartupDiagnostic("FATAL: Zayun API failed to start.", ex);
    Log.Fatal(ex, "Zayun API failed to start.");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
