using System;
using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;
using AssetManager.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;
using System.Windows.Data;
using System.Globalization;

namespace AssetManager.Desktop
{
    public partial class App : Application
    {
        public IConfiguration Configuration { get; }
        private IHost? _apiHost;  // ✅ Store API host instance

        public App()
        {
            // ✅ Set up configuration to read from appsettings.json
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            Configuration = builder.Build();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // ✅ You can register additional services here if needed
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // ✅ Start the API in a background task
            _apiHost = Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.Configure(app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(endpoints =>
                        {
                            // ✅ Test API endpoint
                            endpoints.MapGet("/", async context =>
                            {
                                await context.Response.WriteAsync("Asset Manager API is running!");
                            });

                            // ✅ Get selected model
                            endpoints.MapGet("/api/models/selected_model", async context =>
                            {
                                await context.Response.WriteAsJsonAsync(new { model_id = "test123" });
                            });

                            // ✅ Download model file
                            endpoints.MapGet("/api/models/download/{modelId}", async context =>
                            {
                                string modelId = context.Request.RouteValues["modelId"]?.ToString() ?? "default";
                                string filePath = $"C:\\AssetManager\\Models\\{modelId}.f3d";

                                if (!System.IO.File.Exists(filePath))
                                {
                                    context.Response.StatusCode = 404;
                                    await context.Response.WriteAsync("Model not found.");
                                    return;
                                }

                                var fileStream = System.IO.File.OpenRead(filePath);
                                context.Response.ContentType = "application/octet-stream";
                                await fileStream.CopyToAsync(context.Response.Body);
                            });
                        });
                    });
                })
                .Build();

            await _apiHost.StartAsync();  // ✅ Start API server asynchronously
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            if (_apiHost != null)
            {
                await _apiHost.StopAsync();  // ✅ Gracefully stop API server
                _apiHost.Dispose();
            }
            base.OnExit(e);
        }

    }
}
