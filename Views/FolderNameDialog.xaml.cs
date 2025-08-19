using System.Windows;

namespace Admin_Tasks.Views
{
    /// <summary>
    /// Interaktionslogik für FolderNameDialog.xaml
    /// </summary>
    public partial class FolderNameDialog : Window
    {
        public string FolderName => FolderNameTextBox.Text.Trim();
        
        public FolderNameDialog()
        {
            InitializeComponent();
            FolderNameTextBox.Focus();
        }
        
        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(FolderName))
            {
                MessageBox.Show("Bitte geben Sie einen Ordnernamen ein.", "Eingabe erforderlich",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                FolderNameTextBox.Focus();
                return;
            }
            
            DialogResult = true;
            Close();
        }
        
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}