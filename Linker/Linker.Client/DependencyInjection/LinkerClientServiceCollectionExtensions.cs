using Linker.Client.Contracts;
using Linker.Client.Registration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Linker.Client.DependencyInjection;

public static class LinkerClientServiceCollectionExtensions
{
    public static IServiceCollection AddLinkerClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<LinkerOptions>()
            .Bind(
                configuration.GetSection(
                    LinkerOptions.SectionName))
            .Validate(
                ValidateOptions,
                "Linker 配置无效")
            .ValidateOnStart();

        services.AddSingleton<
            LinkerEndpointBuilder>();

        services.AddSingleton<
            LinkerConnectionManager>();

        services.AddHostedService(
            serviceProvider =>
                serviceProvider.GetRequiredService<
                    LinkerConnectionManager>());

        return services;
    }

    private static bool ValidateOptions(
        LinkerOptions options)
    {
        if (string.IsNullOrWhiteSpace(
            options.ServiceName))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(
            options.AgentHost))
        {
            return false;
        }

        if (options.AgentPort is < 1 or > 65535)
        {
            return false;
        }

        if (options.ConfigKeys is null)
        {
            return false;
        }

        return true;
    }
}