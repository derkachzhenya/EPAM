using Epam.ItMarathon.ApiService.Api.Extension;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendCors", policy =>
    {
        policy
            .WithOrigins(
                "http://3.66.16.191",
                "http://3.126.116.73",
                "https://app-alb-584806949.eu-central-1.elb.amazonaws.com"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder = builder.ConfigureApplicationBuilder();

var app = builder.Build();

app.UseCors("FrontendCors");

app = app.ConfigureApplication();

// Запуск
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
