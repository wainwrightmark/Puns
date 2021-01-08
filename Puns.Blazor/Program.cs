using System;
using System.Net.Http;
using System.Threading.Tasks;
using MatBlazor;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Puns.Blazor
{

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("#app");

        builder.Services.AddScoped(
            _ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) }
        );

        builder.Services.AddMatBlazor();

        await builder.Build().RunAsync();
    }
}

}
