using Admin_Tasks.ViewModels;
using Admin_Tasks.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace Admin_Tasks.Views
{
    /// <summary>
    /// Interaktionslogik für MainView.xaml
    /// </summary>
    public partial class MainView : Window
    {
        private readonly IServiceProvider _serviceProvider;
        
        public MainView(MainViewModel viewModel, IServiceProvider serviceProvider)
        {
            InitializeComponent();
            DataContext = viewModel;
            _serviceProvider = serviceProvider;
            
            // Event-Handler für ViewModel-Events
            viewModel.LogoutRequested += OnLogoutRequested;
            viewModel.TaskEditRequested += OnTaskEditRequested;
            viewModel.TaskCreateRequested += OnTaskCreateRequested;
        }
        
        private void OnLogoutRequested(object? sender, EventArgs e)
        {
            // Hauptfenster schließen und Login anzeigen
            var app = (App)Application.Current;
            app.ShowLoginWindow();
            Close();
        }
        
        private void OnTaskEditRequested(object? sender, TaskItem task)
        {
            try
            {
                var taskEditViewModel = _serviceProvider.GetRequiredService<TaskEditViewModel>();
                
                if (task != null)
                {
                    taskEditViewModel.LoadTask(task);
                }
                
                var taskEditView = new TaskEditView(taskEditViewModel)
                {
                    Owner = this
                };
                
                if (taskEditView.ShowDialog() == true)
                {
                    // Aufgabenliste aktualisieren
                    if (DataContext is MainViewModel vm)
                    {
                        _ = vm.RefreshCommand.ExecuteAsync(null);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Öffnen der Aufgabenbearbeitung: {ex.Message}", 
                               "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void OnTaskCreateRequested(object? sender, EventArgs e)
        {
            try
            {
                var taskEditViewModel = _serviceProvider.GetRequiredService<TaskEditViewModel>();
                taskEditViewModel.PrepareForNewTask();
                
                var taskEditView = new TaskEditView(taskEditViewModel)
                {
                    Owner = this
                };
                
                if (taskEditView.ShowDialog() == true)
                {
                    // Aufgabenliste aktualisieren
                    if (DataContext is MainViewModel vm)
                    {
                        _ = vm.RefreshCommand.ExecuteAsync(null);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Erstellen einer neuen Aufgabe: {ex.Message}", 
                               "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        protected override void OnClosed(EventArgs e)
        {
            // Event-Handler entfernen
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.LogoutRequested -= OnLogoutRequested;
                viewModel.TaskEditRequested -= OnTaskEditRequested;
                viewModel.TaskCreateRequested -= OnTaskCreateRequested;
            }
            base.OnClosed(e);
        }
    }
}