using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PdfService.DAL;
using PdfService.Services;
using System.Configuration;
using System.Data;

namespace PdfService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            

            builder.Services.AddControllers();

            var connectionString = builder.Configuration.GetConnectionString("PgConnection");
            if(string.IsNullOrEmpty(connectionString) ) throw new InvalidOperationException("'PgConnection' not found.");

            builder.Services.AddDbContext<AppDBContext>
                (options => options.UseNpgsql(connectionString));

            builder.Services.AddTransient<IContentService, ContentService>();
            builder.Services.AddSingleton<IQueueService, RabbitService>();
            builder.Services.AddHostedService(p => p.GetRequiredService<IQueueService>());

            builder.Services.AddHostedService<Worker>();

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDBContext>();
                db.Database.Migrate();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
