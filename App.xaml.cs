using System.Configuration;
using System.Data;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
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
    public static IServiceProvider ServiceProvider { get; private set; } = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        // Setup global exception handling
        SetupExceptionHandling();
        
        // Create host with dependency injection
        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                // Database with fallback
                var databaseProvider = context.Configuration["DatabaseProvider"] ?? "SQLite";
                services.AddDbContext<AdminTasksDbContext>(options =>
                {
                    try
                    {
                        if (databaseProvider == "PostgreSQL")
                        {
                            options.UseNpgsql(context.Configuration.GetConnectionString("DefaultConnection"));
                        }
                        else
                        {
                            options.UseSqlite(context.Configuration.GetConnectionString("SqliteConnection"));
                        }
                    }
                    catch
                    {
                        // Fallback to SQLite if PostgreSQL fails
                        options.UseSqlite(context.Configuration.GetConnectionString("SqliteConnection"));
                    }
                });

                // Services
                services.AddScoped<IDatabaseService, DatabaseService>();
                services.AddScoped<IAuthenticationService, AuthenticationService>();
                services.AddScoped<ITaskService, TaskService>();
                services.AddScoped<IAttachmentService, AttachmentService>();
                services.AddScoped<IChatService, ChatService>();
                services.AddScoped<INotificationService, NotificationService>();
                services.AddScoped<ICategoryService, CategoryService>();
                // services.AddScoped<IUserService, UserService>(); // TODO: Implement UserService if needed
                services.AddSingleton<ISettingsService, SettingsService>();
                services.AddSingleton<IThemeService, ThemeService>();
                services.AddSingleton<ICustomFolderService, CustomFolderService>();
                services.AddSingleton<SignalRService>();

                // ViewModels
            services.AddTransient<LoginViewModel>();
            services.AddTransient<MainViewModel>();
            services.AddTransient<TaskDetailViewModel>();
            services.AddTransient<TaskEditViewModel>();
            services.AddTransient<TaskCompletionViewModel>();
            services.AddTransient<UserManagementViewModel>();
            services.AddTransient<ChatViewModel>();
            services.AddSingleton<ChatOverviewViewModel>();

                // Views registrieren
            services.AddTransient<LoginView>();
            services.AddTransient<MainView>(provider => 
                new MainView(
                    provider.GetRequiredService<MainViewModel>(), 
                    provider
                ));
            services.AddTransient<TaskEditView>(provider => 
                new TaskEditView(provider.GetRequiredService<TaskEditViewModel>()));
            services.AddSingleton<ChatOverviewView>(provider => 
                new ChatOverviewView(provider.GetRequiredService<ChatOverviewViewModel>()));
            })
            .Build();

        // Set ServiceProvider for static access
        ServiceProvider = _host.Services;

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

        // Initialize services
        var settingsService = _host.Services.GetRequiredService<ISettingsService>();
        try
        {
            await settingsService.InitializeAsync();

            var themeService = _host.Services.GetRequiredService<IThemeService>();
            await themeService.InitializeThemeAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Fehler bei der Dienst-Initialisierung: {ex.Message}");
            // Theme-Fehler sind nicht kritisch, Anwendung kann trotzdem starten
        }
        
        var authenticationService = _host.Services.GetRequiredService<IAuthenticationService>();
        var loginViewModel = _host.Services.GetRequiredService<LoginViewModel>();

        // Check if auto-login should be performed using the correct key
        var rememberMe = await settingsService.GetSettingAsync<bool>("rememberMe");
        if (rememberMe)
        {
            await loginViewModel.LoadSavedCredentialsAsync();
            if (!string.IsNullOrEmpty(loginViewModel.Username) && !string.IsNullOrEmpty(loginViewModel.Password))
            {
                var user = await authenticationService.LoginAsync(loginViewModel.Username, loginViewModel.Password);
                if (user != null)
                {
                    ShowMainWindow();
                    return; // Auto-login successful, skip showing login view
                }
            }
        }

        // If auto-login fails or is not enabled, show the login window
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

    private void SetupExceptionHandling()
    {
        // Handle unhandled exceptions in the main UI thread
        this.DispatcherUnhandledException += (sender, e) =>
        {
            System.Diagnostics.Debug.WriteLine($"[App] UI Thread Exception: {e.Exception}");
            LogException("UI Thread", e.Exception);
            
            // Prevent application crash for non-critical errors
            if (!IsCriticalException(e.Exception))
            {
                e.Handled = true;
                MessageBox.Show($"Ein Fehler ist aufgetreten: {e.Exception.Message}\n\nDetails wurden protokolliert.", 
                               "Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        };

        // Handle unhandled exceptions in background threads
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            var exception = e.ExceptionObject as Exception;
            System.Diagnostics.Debug.WriteLine($"[App] Background Thread Exception: {exception}");
            LogException("Background Thread", exception);
        };

        // Handle task exceptions
        TaskScheduler.UnobservedTaskException += (sender, e) =>
        {
            System.Diagnostics.Debug.WriteLine($"[App] Task Exception: {e.Exception}");
            LogException("Task", e.Exception);
            e.SetObserved(); // Prevent application crash
        };
    }

    private void LogException(string source, Exception exception)
    {
        try
        {
            var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{source}] {exception?.GetType().Name}: {exception?.Message}\n" +
                           $"StackTrace: {exception?.StackTrace}\n" +
                           $"InnerException: {exception?.InnerException?.Message}\n" +
                           new string('-', 80);
            
            System.Diagnostics.Debug.WriteLine(logMessage);
            
            // Optional: Write to file for production
            // File.AppendAllText("error.log", logMessage + Environment.NewLine);
        }
        catch
        {
            // Ignore logging errors to prevent infinite loops
        }
    }

    private bool IsCriticalException(Exception exception)
    {
        return exception is OutOfMemoryException ||
               exception is StackOverflowException ||
               exception is AccessViolationException ||
               exception is AppDomainUnloadedException;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _host?.Dispose();
        base.OnExit(e);
    }


}

