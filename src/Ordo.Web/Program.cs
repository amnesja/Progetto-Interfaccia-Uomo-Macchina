using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ordo.Infrastructure;
using Ordo.Services;

namespace Ordo.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            using (var scope = host.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<OrdoDbContext>();

                if (context.Database.IsRelational())
                {
                    context.Database.Migrate();             // applica automaticamente le migration mancanti
                }
                else
                {
                    context.Database.EnsureCreated(); // per InMemory durante i test/dev
                }

                DataGenerator.InitializeUsers(context);  // poi esegue il seeding, in sicurezza
            }

            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureKestrel(kestrel =>
                    {
                        kestrel.AddServerHeader = false; // OWASP: Remove Kestrel response header 
                    });

                    webBuilder.UseStartup<Startup>();
                });
    }
}