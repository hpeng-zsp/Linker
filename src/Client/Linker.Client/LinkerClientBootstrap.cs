using Linker.Core.Attributes;
using Linker.Core.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Linker.Client;

[AppStartup(200)]
public sealed class LinkerClientBootstrap : IServiceModule
{
    public void ConfigureServices(
        IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var options = new LinkerClientOptions();

        var section = configuration.GetSection("Linker:Client");
        if (section.Exists())
        {
            section.Bind(options);
        }

        services.AddSingleton(options);
        services.AddSingleton<LinkerClientMemoryStore>();
        services.AddSingleton<LinkerClientManager>();
        services.AddSingleton<LinkerGrpcClientGateway>();
    }
}
