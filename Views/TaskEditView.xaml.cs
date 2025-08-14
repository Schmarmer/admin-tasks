using Admin_Tasks.ViewModels;
using Admin_Tasks.Models;
using System.Windows;

namespace Admin_Tasks.Views
{
    /// <summary>
    /// Interaktionslogik für TaskEditView.xaml
    /// </summary>
    public partial class TaskEditView : Window
    {
        public TaskEditView(TaskEditViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            
            // Event-Handler für ViewModel-Events
            viewModel.TaskSaved += OnTaskSaved;
            viewModel.Cancelled += OnEditCancelled;
        }
        
        private void OnTaskSaved(object? sender, TaskItem task)
        {
            DialogResult = true;
            Close();
        }
        
        private void OnEditCancelled(object? sender, EventArgs e)
        {
            DialogResult = false;
            Close();
        }
        
        protected override void OnClosed(EventArgs e)
        {
            // Event-Handler entfernen
            if (DataContext is TaskEditViewModel viewModel)
            {
                viewModel.TaskSaved -= OnTaskSaved;
                viewModel.Cancelled -= OnEditCancelled;
            }
            
            base.OnClosed(e);
        }
    }
}