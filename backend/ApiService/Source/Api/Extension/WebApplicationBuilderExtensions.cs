using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Epam.ItMarathon.ApiService.Api.Dto.Mapping;
using Epam.ItMarathon.ApiService.Api.Filters.Swagger;
using Epam.ItMarathon.ApiService.Application;
using Epam.ItMarathon.ApiService.Infrastructure;
using FluentValidation;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Models;
using Serilog;

namespace Epam.ItMarathon.ApiService.Api.Extension
{
    [ExcludeFromCodeCoverage]
    public static class WebApplicationBuilderExtensions
    {
        public const string FrontendCorsPolicy = "FrontendCors";

        public static WebApplicationBuilder ConfigureApplicationBuilder(this WebApplicationBuilder builder)
        {
            #region Logging

            builder.Host.UseSerilog((hostContext, loggerConfiguration) =>
            {
                var assembly = Assembly.GetEntryAssembly();

                loggerConfiguration
                    .ReadFrom.Configuration(hostContext.Configuration)
                    .Enrich.WithProperty(
                        "Assembly Version",
                        assembly?.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version)
                    .Enrich.WithProperty(
                        "Assembly Informational Version",
                        assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);
            });

            #endregion

            #region CORS 

            var frontendHosts = builder.Configuration
                .GetSection("Options:FrontendHosts")
                .Get<string[]>() ?? Array.Empty<string>();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy(FrontendCorsPolicy, policyBuilder =>
                {
                    if (frontendHosts.Length == 0)
                    {
                        policyBuilder
                            .AllowAnyOrigin()
                            .AllowAnyHeader()
                            .AllowAnyMethod();
                        return;
                    }

                    policyBuilder
                        .WithOrigins(frontendHosts)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });

            #endregion

            #region Serialization

            builder.Services.Configure<JsonOptions>(opt =>
            {
                opt.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                opt.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                opt.SerializerOptions.PropertyNameCaseInsensitive = true;
                opt.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                opt.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            });

            #endregion

            #region Swagger

            var textInfo = CultureInfo.CurrentCulture.TextInfo;

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1",
                    new OpenApiInfo
                    {
                        Version = Assembly.GetExecutingAssembly().GetName().Version!.ToString(3),
                        Title = $"Secret Nick API - {textInfo.ToTitleCase(builder.Environment.EnvironmentName)}",
                        Description = "EPAM IT Marathon 2025 (Secret Nick) WEB API.",
                        License = new OpenApiLicense
                        {
                            Name = "Secret Nick API - License - MIT",
                            Url = new Uri("https://opensource.org/licenses/MIT")
                        }
                    });

                var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
                options.DocInclusionPredicate((name, api) => true);
                options.DocumentFilter<SwaggerTagDocumentFilter>();
            });

            #endregion

            #region Validation

            builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly(), ServiceLifetime.Singleton);

            #endregion

            #region Dependencies

            builder.Services.InjectInfrastructureLayer(builder.Configuration);
            builder.Services.InjectApplicationLayer();

            #endregion

            #region AutoMapper

            builder.Services.ConfigureMapper();

            #endregion

            return builder;
        }

        private static void ConfigureMapper(this IServiceCollection services)
        {
            services.AddAutoMapper(config =>
            {
                config.AddProfile(new RoomMappingProfile());
                config.AddProfile(new UserMappingProfile());
            });
        }
    }
}
