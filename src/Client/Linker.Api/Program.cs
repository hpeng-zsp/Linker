using Linker.Client;
using Linker.Core.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddLinkerCore();

var app = builder.Build();

app.UseLinkerCore();

app.Run();