using System.Reflection;
using Linker.Core.Contracts;
using Linker.Core.Processors;
using Linker.Core.Scanning;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Linker.Core.Extensions;

public static class LinkerCoreExtensions
{
    /// <summary>
    /// 不传程序集时，自动扫描全部可发现程序集。
    /// </summary>
    public static WebApplicationBuilder AddLinkerCore(this WebApplicationBuilder builder)
    {
        return AddLinkerCore(
            builder,
            configure: null,
            assemblies: null);
    }

    /// <summary>
    /// 传入程序集时，只扫描指定程序集。
    /// </summary>
    public static WebApplicationBuilder AddLinkerCore(this WebApplicationBuilder builder, params Assembly[] assemblies)
    {
        return AddLinkerCore(
            builder,
            configure: null,
            assemblies: assemblies);
    }

    /// <summary>
    /// 不传程序集，自动扫描，并配置扫描选项。
    /// </summary>
    public static WebApplicationBuilder AddLinkerCore(this WebApplicationBuilder builder, Action<LinkerOptions> configure)
    {
        return AddLinkerCore(
            builder,
            configure,
            assemblies: null);
    }

    /// <summary>
    /// 配置扫描选项。
    /// 如果传入程序集，则只扫描传入的程序集。
    /// 如果没有传入程序集，则自动扫描。
    /// </summary>
    public static WebApplicationBuilder AddLinkerCore(this WebApplicationBuilder builder, Action<LinkerOptions>? configure, params Assembly[]? assemblies)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var options = new LinkerOptions();

        configure?.Invoke(options);

        var scanAssemblies = ResolveAssemblies(
            assemblies,
            options);

        var scanner = new TypeScanner();

        // 整个应用只扫描一次。
        var catalog = scanner.Scan(scanAssemblies);

        // 将扫描结果保存到 DI 容器。
        builder.Services.AddSingleton(catalog);

        // 所有类型处理器。
        var processors = new ITypeProcessor[]
        {
            new ServiceProcessor(),
            new RepositoryProcessor()
        };

        // 按处理器顺序执行。
        foreach (var processor in processors
            .OrderBy(x => x.Order))
        {
            processor.Process(
                catalog,
                builder.Services);
        }

        // 创建 AppStartup 执行器。
        var startupExecutor =
            new AppStartupExecutor(catalog);

        // 按 AppStartupAttribute.Order
        // 执行所有启动模块的服务注册。
        startupExecutor.ConfigureServices(
            builder.Services,
            builder.Configuration,
            builder.Environment);

        // 保存启动执行器，后续 UseLinkerCore 时使用。
        builder.Services.AddSingleton(
            startupExecutor);

        if (options.EnableScanLog)
        {
            PrintScanLog(
                scanAssemblies,
                catalog);
        }

        return builder;
    }

    /// <summary>
    /// 执行 AppStartup 中的应用配置和中间件配置。
    /// </summary>
    public static WebApplication UseLinkerCore(
        this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var startupExecutor =
            app.Services.GetRequiredService
                <AppStartupExecutor>();

        startupExecutor.Configure(app);

        return app;
    }

    private static IReadOnlyList<Assembly> ResolveAssemblies(
        Assembly[]? specifiedAssemblies,
        LinkerOptions options)
    {
        // 传入程序集时，以传入的程序集为准。
        if (specifiedAssemblies is
            { Length: > 0 })
        {
            return specifiedAssemblies
                .Where(x => x is not null)
                .DistinctBy(x => x.FullName)
                .ToArray();
        }

        // 没有传入程序集，执行自动发现。
        var entryAssembly =
            Assembly.GetEntryAssembly()
            ?? throw new InvalidOperationException(
                "无法获取入口程序集。");

        var assemblies =
            ApplicationAssemblyDiscovery.Discover(
                entryAssembly,
                options);

        if (assemblies.Count == 0 &&
            options.ThrowIfNoAssemblyFound)
        {
            throw new InvalidOperationException(
                "没有发现可扫描的程序集。");
        }

        return assemblies;
    }

    private static void PrintScanLog(
        IReadOnlyList<Assembly> assemblies,
        TypeCatalog catalog)
    {
        Console.WriteLine(
            $"[Linker] 扫描程序集数量：{assemblies.Count}");

        Console.WriteLine(
            $"[Linker] 扫描类型数量：{catalog.Types.Count}");

        Console.WriteLine(
            $"[Linker] Service 数量：{catalog.Services.Count()}");

        Console.WriteLine(
            $"[Linker] Repository 数量：{catalog.Repositories.Count()}");

        Console.WriteLine(
            $"[Linker] AppStartup 数量：{catalog.Startups.Count()}");
    }
}