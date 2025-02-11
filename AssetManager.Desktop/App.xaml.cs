using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;
using AssetManager.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace AssetManager.Desktop;

public partial class App : Application
{
    public IConfiguration Configuration { get; }

    public App()
    {
        // Set up the configuration to read from appsettings.json
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

        Configuration = builder.Build();
    }

    public void ConfigureServices(IServiceCollection services)
    {
        // Register the DbContext with the connection string from the configuration

    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        // Here you can add code to configure and start your application
        LoginWindow loginWindow = new LoginWindow();
        loginWindow.Show();
        
        MainWindow mainWindow = new MainWindow();
        mainWindow.Show();
    }
}