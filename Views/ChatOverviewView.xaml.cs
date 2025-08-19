using System.Threading.Tasks;
using System.Windows.Controls;
using Admin_Tasks.ViewModels;

namespace Admin_Tasks.Views
{
    /// <summary>
    /// Interaktionslogik für ChatOverviewView.xaml
    /// </summary>
    public partial class ChatOverviewView : UserControl
    {
        private bool _isInitialized = false;
        
        public ChatOverviewView()
        {
            InitializeComponent();
        }
        
        public ChatOverviewView(ChatOverviewViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }
        
        // UserControl_Loaded entfernt - Initialisierung erfolgt explizit über InitializeViewModelAsync()
        
        private void UserControl_Unloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is ChatOverviewViewModel viewModel)
            {
                viewModel.Dispose();
            }
        }
        
        public async Task InitializeViewModelAsync()
        {
            if (DataContext is ChatOverviewViewModel viewModel && !_isInitialized)
            {
                _isInitialized = true;
                await viewModel.InitializeAsync();
            }
            // If already initialized, just refresh the existing chats
            else if (DataContext is ChatOverviewViewModel vm)
            {
                // Trigger filtering to show current state
                vm.RefreshCommand.Execute(null);
            }
        }
    }
}