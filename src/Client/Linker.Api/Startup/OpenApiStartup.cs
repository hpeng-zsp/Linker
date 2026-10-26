using Linker.Core.Attributes;
using Linker.Core.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Scalar.AspNetCore;

namespace Linker.Api.Startup;

[AppStartup(800)]
public sealed class OpenApiStartup :
    IServiceModule,
    IApplicationModule
{
    public void ConfigureServices(
        IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddOpenApi();
    }

    public void Configure(WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        app.MapOpenApi();

        app.MapScalarApiReference();
    }
}