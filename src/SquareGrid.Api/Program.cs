using Azure.Identity;
using Microsoft.Azure.Functions.Worker.Extensions.OpenApi.Extensions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Configurations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using SquareGrid.Api;
using SquareGrid.Api.Middleware.Tokens;
using SquareGrid.Common;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults((worker) =>
    {
        worker.UseNewtonsoftJson();
        worker.UseWhen<JwtBearerValidationMiddleware>(JwtBearerValidationMiddleware.IsAuthorizedHttp);
    })
    .ConfigureOpenApi()
    .ConfigureHostConfiguration(configHost =>
    {
        configHost.AddJsonFile("local.settings.json", true);
    })
    .ConfigureServices((hostBuilderContext, services) =>
    {
        services.AddMemoryCache();
        services.AddTransient<B2CConfigurationManager>();

        _ = services
            .AddSingleton<IOpenApiConfigurationOptions>(_ =>
            {
                var version = typeof(Program).Assembly.GetName().Version!.ToString().TrimEnd('0').TrimEnd('.');

                var options = new OpenApiConfigurationOptions()
                {
                    Info = new OpenApiInfo()
                    {
                        Version = version,
                        Title = $"SquareGrid Api",
                        Description = "Api calls and message processing functions for the SquareGrid app",
                    },
                    Servers = DefaultOpenApiConfigurationOptions.GetHostNames(),
                    OpenApiVersion = DefaultOpenApiConfigurationOptions.GetOpenApiVersion(),
                    IncludeRequestingHostName = DefaultOpenApiConfigurationOptions.IsFunctionsRuntimeEnvironmentDevelopment(),
                    ForceHttps = DefaultOpenApiConfigurationOptions.IsHttpsForced(),
                    ForceHttp = DefaultOpenApiConfigurationOptions.IsHttpForced(),
                };

                return options;
            })
            .RegisterCommonDependencies(hostBuilderContext.Configuration);
    })
    .Build();

host.Run();