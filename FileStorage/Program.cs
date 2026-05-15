using FileStorage.Services;

namespace FileStorage
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();

            builder.Services.AddTransient<IStorageService, LocalStorageService>();

            builder.Services.AddSingleton<IQueueService, RabbitService>();
            builder.Services.AddHostedService(p => p.GetRequiredService<IQueueService>());

            builder.Services.AddHostedService<Worker>();

            var app = builder.Build();

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
