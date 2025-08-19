using Admin_Tasks.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Admin_Tasks.Views;

/// <summary>
/// Interaktionslogik für CustomFolderDialog.xaml
/// </summary>
public partial class CustomFolderDialog : Window
{
    public string FolderName { get; set; } = string.Empty;
    public string FolderDescription { get; set; } = string.Empty;
    public string FolderColor { get; set; } = "#2196F3";
    
    private readonly bool _isEditMode;
    private readonly CustomFolder? _existingFolder;

    public CustomFolderDialog()
    {
        InitializeComponent();
        _isEditMode = false;
        InitializeDialog();
    }
    
    public CustomFolderDialog(CustomFolder existingFolder)
    {
        InitializeComponent();
        _isEditMode = true;
        _existingFolder = existingFolder;
        
        // Bestehende Werte laden
        FolderName = existingFolder.Name;
        FolderDescription = existingFolder.Description ?? string.Empty;
        FolderColor = existingFolder.Color;
        
        InitializeDialog();
    }
    
    private void InitializeDialog()
    {
        // Titel anpassen
        Title = _isEditMode ? "Ordner bearbeiten" : "Ordner erstellen";
        
        // DataContext setzen
        DataContext = this;
        
        // Event-Handler für Eingabevalidierung
        FolderNameTextBox.TextChanged += OnTextChanged;
        DescriptionTextBox.TextChanged += OnTextChanged;
        
        // Event-Handler für Farbauswahl
        ColorBlue.Checked += OnColorChanged;
        ColorGreen.Checked += OnColorChanged;
        ColorOrange.Checked += OnColorChanged;
        ColorRed.Checked += OnColorChanged;
        ColorPurple.Checked += OnColorChanged;
        ColorTeal.Checked += OnColorChanged;
        
        // Bestehende Farbe auswählen
        SelectColorRadioButton(FolderColor);
        
        // Vorschau aktualisieren
        UpdatePreview();
        
        // Focus auf Namensfeld
        FolderNameTextBox.Focus();
        FolderNameTextBox.SelectAll();
    }
    
    private void SelectColorRadioButton(string color)
    {
        var radioButtons = new[] { ColorBlue, ColorGreen, ColorOrange, ColorRed, ColorPurple, ColorTeal };
        
        foreach (var rb in radioButtons)
        {
            if (rb.Tag?.ToString() == color)
            {
                rb.IsChecked = true;
                break;
            }
        }
    }
    
    private void OnTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        UpdatePreview();
    }
    
    private void OnColorChanged(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.Tag is string color)
        {
            FolderColor = color;
            UpdatePreview();
        }
    }
    
    private void UpdatePreview()
    {
        // Vorschauname aktualisieren
        PreviewNameText.Text = string.IsNullOrWhiteSpace(FolderName) ? "Neuer Ordner" : FolderName;
        
        // Vorschaufarbe aktualisieren
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(FolderColor);
            PreviewColorBorder.Background = new SolidColorBrush(color);
        }
        catch
        {
            PreviewColorBorder.Background = new SolidColorBrush(Colors.Gray);
        }
    }
    
    private void OK_Click(object sender, RoutedEventArgs e)
    {
        // Validierung
        if (string.IsNullOrWhiteSpace(FolderName))
        {
            MessageBox.Show("Bitte geben Sie einen Ordnernamen ein.", 
                           "Eingabe erforderlich", 
                           MessageBoxButton.OK, 
                           MessageBoxImage.Warning);
            FolderNameTextBox.Focus();
            return;
        }
        
        if (FolderName.Length > 50)
        {
            MessageBox.Show("Der Ordnername darf maximal 50 Zeichen lang sein.", 
                           "Eingabe zu lang", 
                           MessageBoxButton.OK, 
                           MessageBoxImage.Warning);
            FolderNameTextBox.Focus();
            return;
        }
        
        if (FolderDescription.Length > 200)
        {
            MessageBox.Show("Die Beschreibung darf maximal 200 Zeichen lang sein.", 
                           "Eingabe zu lang", 
                           MessageBoxButton.OK, 
                           MessageBoxImage.Warning);
            DescriptionTextBox.Focus();
            return;
        }
        
        // Ungültige Zeichen prüfen
        var invalidChars = System.IO.Path.GetInvalidFileNameChars();
        if (FolderName.IndexOfAny(invalidChars) >= 0)
        {
            MessageBox.Show("Der Ordnername enthält ungültige Zeichen.", 
                           "Ungültige Eingabe", 
                           MessageBoxButton.OK, 
                           MessageBoxImage.Warning);
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
    
    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            OK_Click(sender, e);
        }
        else if (e.Key == Key.Escape)
        {
            Cancel_Click(sender, e);
        }
    }
}