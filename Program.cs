using DispatcherService;
using Microsoft.Extensions.Hosting.WindowsServices;

IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService(options =>
    {
        options.ServiceName = "DispatcherService";
    }
    )
    .ConfigureServices(services =>
    {
        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();
