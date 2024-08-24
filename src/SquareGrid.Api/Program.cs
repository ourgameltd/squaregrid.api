using Microsoft.Azure.Functions.Worker.Extensions.OpenApi.Extensions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Configurations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using SquareGrid.Common;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults((worker) =>
    {
        worker.UseNewtonsoftJson();
    })
    .ConfigureOpenApi()
    .ConfigureHostConfiguration(configHost =>
    {
        configHost.AddJsonFile("local.settings.json", true);
    })
    .ConfigureServices((hostBuilderContext, services) =>
    {
        services.AddMemoryCache();

        _ = services
            .AddSingleton<IOpenApiConfigurationOptions>(_ =>
            {
                var version = typeof(Program).Assembly.GetName().Version!.ToString().TrimEnd('0').TrimEnd('.');

                var options = new OpenApiConfigurationOptions()
                {
                    Info = new OpenApiInfo()
                    {
                        Version = version,
                        Title = $"Square Grid Api",
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