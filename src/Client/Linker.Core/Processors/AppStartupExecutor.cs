using Linker.Core.Contracts;
using Linker.Core.Scanning;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Linker.Core.Processors;

/// <summary>
/// 按 AppStartupAttribute.Order 执行所有启动模块。
/// </summary>
public sealed class AppStartupExecutor
{
    private readonly IReadOnlyList<IServiceModule>
        _serviceModules;

    private readonly IReadOnlyList<IApplicationModule>
        _applicationModules;

    public AppStartupExecutor(
        TypeCatalog catalog)
    {
     var startupTypes = catalog.Startups
            .OrderBy(x => x.StartupOrder)
            .Select(x => x.ImplementationType)
            .ToArray();

        var serviceModules = new List<IServiceModule>();

        var applicationModules =
            new List<IApplicationModule>();

        foreach (var startupType in startupTypes)
        {
            var instance = Activator.CreateInstance(
                startupType);

            if (instance is null)
            {
                throw new InvalidOperationException(
                    $"无法创建启动类型：{startupType.FullName}");
            }

            if (instance is IServiceModule serviceModule)
            {
                serviceModules.Add(serviceModule);
            }

            if (instance is IApplicationModule applicationModule)
            {
                applicationModules.Add(applicationModule);
            }
        }

        _serviceModules = serviceModules;
        _applicationModules = applicationModules;
    }

    public void ConfigureServices(
        IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        foreach (var module in _serviceModules)
        {
            module.ConfigureServices(
                services,
                configuration,
                environment);
        }
    }

    public void Configure(WebApplication app)
    {
        foreach (var module in _applicationModules)
        {
            module.Configure(app);
        }
    }
}