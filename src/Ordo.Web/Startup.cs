//using Ordo.Web.Hubs;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Ordo.Services;
using Ordo.Web.Infrastructure;
using Ordo.Web.SignalR.Hubs;

namespace Ordo.Web
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public IWebHostEnvironment Env { get; set; }

        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Env = env;
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<AppSettings>(Configuration.GetSection("AppSettings"));

            services.AddDbContext<OrdoDbContext>(options =>
            {
                var connectionString = Configuration.GetConnectionString("DefaultConnection")
                                       ?? Environment.GetEnvironmentVariable("ORDO_CONNECTION");

                if (!string.IsNullOrEmpty(connectionString))
                {
                    if (connectionString.Contains("Data Source=") || connectionString.Contains("Filename=") || connectionString.EndsWith(".db"))
                    {
                        options.UseSqlite(connectionString);
                    }
                    else
                    {
                        options.UseSqlServer(connectionString);
                    }
                }
                else
                {
                    // Fallback veloce per sviluppo se non è configurata alcuna connessione
                    options.UseInMemoryDatabase(databaseName: "Ordo");
                }
            });

            // SERVICES FOR AUTHENTICATION
            services.AddSession();
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
            {
                options.LoginPath = "/Login/Login";
                options.LogoutPath = "/Login/Logout";
            });

            var builder = services.AddMvc()
                .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
                .AddDataAnnotationsLocalization(options =>
                {                        // Enable loading SharedResource for ModelLocalizer
                    options.DataAnnotationLocalizerProvider = (type, factory) =>
                        factory.Create(typeof(SharedResource));
                });

#if DEBUG
            builder.AddRazorRuntimeCompilation();
#endif

            services.Configure<RazorViewEngineOptions>(options =>
            {
                options.AreaViewLocationFormats.Clear();
                options.AreaViewLocationFormats.Add("/Areas/{2}/{0}.cshtml");
                options.AreaViewLocationFormats.Add("/Areas/{2}/{1}/{0}.cshtml");
                options.AreaViewLocationFormats.Add("/Areas/{2}/Views/{1}/{0}.cshtml");
                options.AreaViewLocationFormats.Add("/Areas/{2}/Views/Shared/{0}.cshtml");
                options.AreaViewLocationFormats.Add("/Views/Shared/{0}.cshtml");

                options.ViewLocationFormats.Clear();
                options.ViewLocationFormats.Add("/Features/{1}/{0}.cshtml");
                options.ViewLocationFormats.Add("/Features/Views/{1}/{0}.cshtml");
                options.ViewLocationFormats.Add("/Features/Views/Shared/{0}.cshtml");
                options.ViewLocationFormats.Add("/Views/Shared/{0}.cshtml");
            });

            // SIGNALR FOR COLLABORATIVE PAGES
            services.AddSignalR();

            // CONTAINER FOR ALL EXTRA CUSTOM SERVICES
            Container.RegisterTypes(services);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Configure the HTTP request pipeline.
            if (!env.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");

                // Https redirection only in production
                app.UseHsts();
                app.UseHttpsRedirection();
            }

            // Localization support if you want to
            app.UseRequestLocalization(SupportedCultures.CultureNames);

            app.UseRouting();

            // Adding authentication to pipeline
            app.UseSession();
            app.UseAuthentication();
            app.UseAuthorization();

            var node_modules = new CompositePhysicalFileProvider(Directory.GetCurrentDirectory(), "node_modules");
            var areas = new CompositePhysicalFileProvider(Directory.GetCurrentDirectory(), "Areas");
            var compositeFp = new CustomCompositeFileProvider(env.WebRootFileProvider, node_modules, areas);
            env.WebRootFileProvider = compositeFp;
            app.UseStaticFiles();

            app.UseEndpoints(endpoints =>
            {
                // ROUTING PER HUB
                endpoints.MapHub<OrdoHub>("/OrdoHub");

                // ROUTE DELLE AREE - Semplificata per Progetti
                endpoints.MapAreaControllerRoute(
                    name: "Progetti", 
                    areaName: "Progetti", 
                    pattern: "Progetti/{action=Index}/{id?}", 
                    defaults: new { controller = "Progetti" });
                
                // ROUTE DELLE AREE - Semplificata per Kanban
                endpoints.MapAreaControllerRoute(
                    name: "Kanban",
                    areaName: "Kanban",
                    pattern: "Kanban/{action=Index}/{id?}",
                    defaults: new { controller = "Kanban" }
                );
               
                // ROUTE DELLE AREE - Semplificata per Tasks
                endpoints.MapAreaControllerRoute(
                    name: "Tasks",
                    areaName: "Tasks",
                    pattern: "Tasks/{action=Dettaglio}/{id?}",
                    defaults: new { controller = "Tasks" });                
                

                // ROUTE GENERICHE
                endpoints.MapControllerRoute("dashboard", "Dashboard/{action=Index}/{id?}", new { controller = "Dashboard" });
                endpoints.MapControllerRoute("profile", "Profile/{action=Profile}/{id?}", new { controller = "Profile" });
                endpoints.MapControllerRoute("attivita", "Attivita/{action=Index}", new { controller = "Attivita" });
                endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}");
            });
        }
    }

    public static class SupportedCultures
    {
        public readonly static string[] CultureNames;
        public readonly static CultureInfo[] Cultures;

        static SupportedCultures()
        {
            CultureNames = new[] { "it-it" };
            Cultures = CultureNames.Select(c => new CultureInfo(c)).ToArray();

            //NB: attenzione nel progetto a settare correttamente <NeutralLanguage>it-IT</NeutralLanguage>
        }
    }
}
