
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

namespace gateway
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Configuration
                .SetBasePath(builder.Environment.ContentRootPath)
                .AddOcelot(); 
            builder.Services
                .AddOcelot(builder.Configuration);

            var app = builder.Build();
            await app.UseOcelot();

            app.UseHttpsRedirection();

            await app.RunAsync();
        }
    }
}
