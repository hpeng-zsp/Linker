using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Linker.Core.Contracts;

public interface IServiceModule
{
    void ConfigureServices(
        IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment);
}