using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Admin_Tasks.ViewModels;

namespace Admin_Tasks.Views
{
    /// <summary>
    /// Interaction logic for ChatControl.xaml
    /// </summary>
    public partial class ChatControl : UserControl
    {
        public ChatControl()
        {
            InitializeComponent();
        }
        
        private ChatViewModel? ViewModel => DataContext as ChatViewModel;
        
        private void SendMessage_Click(object sender, RoutedEventArgs e)
        {
            ViewModel?.SendMessageCommand?.Execute(null);
            MessageTextBox.Focus();
        }
        
        private void MessageTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (Keyboard.Modifiers == ModifierKeys.Control)
                {
                    // Ctrl+Enter: Send message
                    ViewModel?.SendMessageCommand?.Execute(null);
                    e.Handled = true;
                }
                else if (Keyboard.Modifiers == ModifierKeys.None)
                {
                    // Enter: New line (default behavior)
                    // Let the TextBox handle this naturally
                }
            }
        }
        
        private void CancelReply_Click(object sender, RoutedEventArgs e)
        {
            ViewModel?.CancelReplyCommand?.Execute(null);
        }
        
        /// <summary>
        /// Scrolls to the bottom of the chat when new messages arrive
        /// </summary>
        public void ScrollToBottom()
        {
            ChatScrollViewer.ScrollToBottom();
        }
        
        /// <summary>
        /// Sets focus to the message input box
        /// </summary>
        public void FocusMessageInput()
        {
            MessageTextBox.Focus();
        }
    }
}