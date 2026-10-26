using Microsoft.AspNetCore.Builder;

namespace Linker.Core.Contracts;

public interface IApplicationModule
{
    void Configure(WebApplication app);
}