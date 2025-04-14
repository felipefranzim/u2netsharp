using U2NetSharp.Server;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateDefaultBuilder(args);
builder.ConfigureServices((hostContext, services) =>
 {
     services.AddHostedService<Worker>();
 })
 .UseWindowsService();

var host = builder.Build();
host.Run();
