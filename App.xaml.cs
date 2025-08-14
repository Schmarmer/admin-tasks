using System.Configuration;
using System.Data;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Admin_Tasks.Models;
using Admin_Tasks.Services;
using Admin_Tasks.ViewModels;
using Admin_Tasks.Views;

namespace Admin_Tasks;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        // Create host with dependency injection
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Database
                services.AddDbContext<AdminTasksDbContext>(options =>
                    options.UseSqlite("Data Source=AdminTasks.db"));

                // Services
                services.AddScoped<IDatabaseService, DatabaseService>();
                services.AddScoped<IAuthenticationService, AuthenticationService>();
                services.AddScoped<ITaskService, TaskService>();
                services.AddSingleton<IThemeService, ThemeService>();

                // ViewModels
                services.AddTransient<LoginViewModel>();
                services.AddTransient<MainViewModel>();
                services.AddTransient<TaskEditViewModel>();

                // Views registrieren
            services.AddTransient<LoginView>();
            services.AddTransient<MainView>(provider => 
                new MainView(
                    provider.GetRequiredService<MainViewModel>(), 
                    provider
                ));
            services.AddTransient<TaskEditView>();
            })
            .Build();

        // Initialize database
        try
        {
            using var scope = _host.Services.CreateScope();
            var databaseService = scope.ServiceProvider.GetRequiredService<IDatabaseService>();
            await databaseService.InitializeDatabaseAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fehler bei der Datenbankinitialisierung: {ex.Message}", 
                          "Datenbankfehler", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
            return;
        }

        // Show login window
        var loginView = _host.Services.GetRequiredService<LoginView>();
        loginView.Show();
        
        base.OnStartup(e);
    }

    public void ShowMainWindow()
    {
        var mainView = _host.Services.GetRequiredService<MainView>();
        mainView.Show();
    }
    
    public void ShowLoginWindow()
    {
        var loginView = _host.Services.GetRequiredService<LoginView>();
        loginView.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _host?.Dispose();
        base.OnExit(e);
    }

    public static IServiceProvider ServiceProvider => ((App)Current)._host?.Services ?? throw new InvalidOperationException("Services not initialized");
}

