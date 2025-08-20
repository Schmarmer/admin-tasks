using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Admin_Tasks.Models;

namespace Admin_Tasks.Views
{
    public partial class UserSelectionDialog : Window, INotifyPropertyChanged
    {
        private User? _selectedUser;
        
        public UserSelectionDialog(ObservableCollection<User> users, User? currentUser = null)
        {
            InitializeComponent();
            DataContext = this;
            
            // Filter out current user from the list
            Users = new ObservableCollection<User>();
            foreach (var user in users)
            {
                if (currentUser == null || user.Id != currentUser.Id)
                {
                    Users.Add(user);
                }
            }
        }
        
        public ObservableCollection<User> Users { get; }
        
        public User? SelectedUser
        {
            get => _selectedUser;
            set
            {
                _selectedUser = value;
                OnPropertyChanged();
            }
        }
        
        // Use base.DialogResult so ShowDialog() in caller receives the correct result
        private void ForwardButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedUser != null)
            {
                base.DialogResult = true; // This will close the dialog automatically
            }
        }
        
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            base.DialogResult = false; // This will close the dialog automatically
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}