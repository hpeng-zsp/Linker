using Linker.Core.Attributes;
using Linker.Core.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Linker.Api.Startup;

[AppStartup(900)]
public sealed class WebStartup :
    IServiceModule,
    IApplicationModule
{
    public void ConfigureServices(
        IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddControllers();
    }

    public void Configure(WebApplication app)
    {
        app.UseHttpsRedirection();

        app.MapControllers();
    }
}