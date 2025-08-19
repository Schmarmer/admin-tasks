using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Admin_Tasks.ViewModels;
using Microsoft.Win32;

namespace Admin_Tasks.Views;

public partial class TaskCompletionDialog : Window
{
    public TaskCompletionViewModel ViewModel { get; private set; }
    
    public TaskCompletionDialog(TaskCompletionViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = ViewModel;
        
        // Initialize UI state
        InitializeRatingButtons();
        
        // Event handlers
        Loaded += TaskCompletionDialog_Loaded;
    }
    
    private void TaskCompletionDialog_Loaded(object sender, RoutedEventArgs e)
    {
        // Focus on the first input field
        HoursTextBox.Focus();
        HoursTextBox.SelectAll();
    }
    
    private void InitializeRatingButtons()
    {
        // Initialize difficulty rating buttons
        UpdateDifficultyRatingButtons(ViewModel.DifficultyRating);
        
        // Initialize satisfaction rating buttons
        UpdateSatisfactionRatingButtons(ViewModel.SatisfactionRating);
    }
    
    private void DifficultyRating_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && int.TryParse(button.Tag?.ToString(), out int rating))
        {
            ViewModel.DifficultyRating = rating;
            UpdateDifficultyRatingButtons(rating);
        }
    }
    
    private void SatisfactionRating_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && int.TryParse(button.Tag?.ToString(), out int rating))
        {
            ViewModel.SatisfactionRating = rating;
            UpdateSatisfactionRatingButtons(rating);
        }
    }
    
    private void UpdateDifficultyRatingButtons(int selectedRating)
    {
        var buttons = new[] { Difficulty1, Difficulty2, Difficulty3, Difficulty4, Difficulty5 };
        
        for (int i = 0; i < buttons.Length; i++)
        {
            var button = buttons[i];
            var rating = i + 1;
            
            if (rating <= selectedRating)
            {
                // Selected state
                button.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF4CAF50"));
                button.Foreground = Brushes.White;
                button.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF4CAF50"));
            }
            else
            {
                // Unselected state
                button.Background = Brushes.Transparent;
                button.Foreground = (Brush)FindResource("TextBrush");
                button.BorderBrush = (Brush)FindResource("BorderBrush");
            }
        }
    }
    
    private void UpdateSatisfactionRatingButtons(int selectedRating)
    {
        var buttons = new[] { Satisfaction1, Satisfaction2, Satisfaction3, Satisfaction4, Satisfaction5 };
        
        for (int i = 0; i < buttons.Length; i++)
        {
            var button = buttons[i];
            var rating = i + 1;
            
            if (rating <= selectedRating)
            {
                // Selected state
                button.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF2196F3"));
                button.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF2196F3"));
            }
            else
            {
                // Unselected state
                button.Background = Brushes.Transparent;
                button.BorderBrush = (Brush)FindResource("BorderBrush");
            }
        }
    }
    
    private void SelectImage_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Title = "Abschlussbild auswählen",
            Filter = "Bilddateien (*.jpg;*.jpeg;*.png;*.bmp;*.gif)|*.jpg;*.jpeg;*.png;*.bmp;*.gif|Alle Dateien (*.*)|*.*",
            FilterIndex = 1,
            Multiselect = false
        };
        
        if (openFileDialog.ShowDialog() == true)
        {
            try
            {
                var filePath = openFileDialog.FileName;
                var fileName = Path.GetFileName(filePath);
                
                // Validate file size (max 10MB)
                var fileInfo = new FileInfo(filePath);
                if (fileInfo.Length > 10 * 1024 * 1024)
                {
                    MessageBox.Show(
                        "Die ausgewählte Datei ist zu groß. Bitte wählen Sie eine Datei mit maximal 10 MB.",
                        "Datei zu groß",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }
                
                // Set image in ViewModel
                ViewModel.SetCompletionImage(filePath, fileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fehler beim Laden des Bildes: {ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
    
    private void RemoveImage_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.RemoveCompletionImage();
    }
    
    private async void CompleteButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Validate input
            if (!ViewModel.ValidateInput())
            {
                MessageBox.Show(
                    "Bitte füllen Sie alle erforderlichen Felder aus:\n\n" +
                    "• Zeiterfassung (mindestens 1 Minute)\n" +
                    "• Abschlussbericht\n" +
                    "• Schwierigkeitsgrad\n" +
                    "• Zufriedenheitsbewertung",
                    "Eingabe unvollständig",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }
            
            // Disable button to prevent double-click
            var button = sender as Button;
            if (button != null)
            {
                button.IsEnabled = false;
                button.Content = "⏳ Wird abgeschlossen...";
            }
            
            // Complete the task
            var success = await ViewModel.CompleteTaskAsync();
            
            if (success)
            {
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show(
                    "Fehler beim Abschließen der Aufgabe. Bitte versuchen Sie es erneut.",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                
                // Re-enable button
                if (button != null)
                {
                    button.IsEnabled = true;
                    button.Content = "✅ Aufgabe abschließen";
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Unerwarteter Fehler beim Abschließen der Aufgabe: {ex.Message}",
                "Fehler",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            
            // Re-enable button
            var button = sender as Button;
            if (button != null)
            {
                button.IsEnabled = true;
                button.Content = "✅ Aufgabe abschließen";
            }
        }
    }
    
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        // Ask for confirmation if user has entered data
        if (ViewModel.HasUnsavedChanges())
        {
            var result = MessageBox.Show(
                "Sie haben Änderungen vorgenommen, die nicht gespeichert wurden. Möchten Sie wirklich abbrechen?",
                "Ungespeicherte Änderungen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            
            if (result != MessageBoxResult.Yes)
            {
                return;
            }
        }
        
        DialogResult = false;
        Close();
    }
    
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        CancelButton_Click(sender, e);
    }
    
    protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
    {
        // Handle Escape key
        if (e.Key == System.Windows.Input.Key.Escape)
        {
            CancelButton_Click(this, new RoutedEventArgs());
            e.Handled = true;
            return;
        }
        
        // Handle Ctrl+Enter for quick completion
        if (e.Key == System.Windows.Input.Key.Enter && 
            (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) == System.Windows.Input.ModifierKeys.Control)
        {
            if (ViewModel.CanComplete)
            {
                CompleteButton_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
        }
        
        base.OnKeyDown(e);
    }
}