using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RetailShop.Core.Interfaces;
using RetailShop.Data;
using RetailShop.UI.Forms;
using RetailShop.UI.Services;

namespace RetailShop.UI;

static class Program
{
    public static IHost? AppHost { get; private set; }

    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (s, e) =>
        {
            try
            {
                var logMsg = $"[{DateTime.UtcNow:u}] UI Thread Exception: {e.Exception.Message}\n{e.Exception.StackTrace}\n\n";
                File.AppendAllText("error.log", logMsg);
                Console.WriteLine(logMsg);
            }
            catch { }
        };

        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            try
            {
                var ex = e.ExceptionObject as Exception;
                var logMsg = $"[{DateTime.UtcNow:u}] AppDomain Unhandled Exception: {ex?.Message}\n{ex?.StackTrace}\n\n";
                File.AppendAllText("error.log", logMsg);
                Console.WriteLine(logMsg);
            }
            catch { }
        };

        var builder = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                config.SetBasePath(AppContext.BaseDirectory);
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                // Register Core & Data services
                services.AddRetailShopData();

                // Register UI Services & Forms
                services.AddSingleton<ReceiptPrinter>();
                services.AddSingleton<PdfReportService>();
                services.AddTransient<LoginForm>();
                services.AddTransient<MainDashboardForm>();
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
            });

        AppHost = builder.Build();

        // Run database migration and seeding on startup
        try
        {
            using var scope = AppHost.Services.CreateScope();
            var dbInitializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
            dbInitializer.InitializeAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Database initialization failed.\nPlease ensure Microsoft SQL Server / SQLEXPRESS is running.\n\nError details:\n{ex.Message}",
                "Database Initialization Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        // Launch Login
        var loginForm = AppHost.Services.GetRequiredService<LoginForm>();
        Application.Run(loginForm);
    }
}