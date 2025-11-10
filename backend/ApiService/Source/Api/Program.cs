using Epam.ItMarathon.ApiService.Api.Extension;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ✅ ДОБАВЛЯЕМ ЧТЕНИЕ ENV
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

builder = builder.ConfigureApplicationBuilder();

var app = builder
    .Build()
    .ConfigureApplication();

try
{
    Log.Information("Starting host");
    app.Run();
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}
