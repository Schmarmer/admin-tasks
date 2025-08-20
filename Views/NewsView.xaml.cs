using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Admin_Tasks.Models;
using Admin_Tasks.ViewModels;

namespace Admin_Tasks.Views;

/// <summary>
/// Interaktionslogik für NewsView.xaml
/// News-Seite für die Anzeige von Benachrichtigungen
/// </summary>
public partial class NewsView : UserControl
{
    public NewsView(NewsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
    
    public NewsView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Event-Handler für Klick auf News-Item
    /// Löst die Benachrichtigungsanzeige aus und markiert als gelesen
    /// </summary>
    /// <param name="sender">Das geklickte Border-Element</param>
    /// <param name="e">Mouse-Event-Argumente</param>
    private void NewsItem_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.Tag is Notification notification)
        {
            // ViewModel abrufen
            if (DataContext is NewsViewModel viewModel)
            {
                // OnClick-Event für Benachrichtigung auslösen
                viewModel.HandleNewsItemClickCommand?.Execute(notification);
            }
        }
    }



    /// <summary>
    /// Event-Handler für Rechtsklick auf News-Item
    /// Zeigt Kontextmenü mit Aktionen an
    /// </summary>
    /// <param name="sender">Das geklickte Border-Element</param>
    /// <param name="e">Mouse-Event-Argumente</param>
    private void NewsItem_RightClick(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Right && sender is Border border && border.Tag is Notification notification)
        {
            // Kontextmenü erstellen
            var contextMenu = new ContextMenu();

            // "Als gelesen/ungelesen markieren" MenuItem
            var toggleReadMenuItem = new MenuItem
            {
                Header = notification.IsRead ? "Als ungelesen markieren" : "Als gelesen markieren",
                Icon = new TextBlock { Text = notification.IsRead ? "📧" : "✓", FontFamily = new System.Windows.Media.FontFamily("Segoe UI Emoji") }
            };
            toggleReadMenuItem.Click += (s, args) =>
            {
                if (DataContext is NewsViewModel viewModel)
                {
                    viewModel.ToggleReadStatusCommand?.Execute(notification);
                }
            };
            contextMenu.Items.Add(toggleReadMenuItem);

            // Separator
            contextMenu.Items.Add(new Separator());

            // "Zur Aufgabe navigieren" MenuItem (nur wenn Task vorhanden)
            if (notification.TaskId.HasValue)
            {
                var navigateToTaskMenuItem = new MenuItem
                {
                    Header = "Zur Aufgabe navigieren",
                    Icon = new TextBlock { Text = "📋", FontFamily = new System.Windows.Media.FontFamily("Segoe UI Emoji") }
                };
                navigateToTaskMenuItem.Click += (s, args) =>
                {
                    if (DataContext is NewsViewModel viewModel)
                    {
                        viewModel.NavigateToTaskCommand?.Execute(notification.TaskId.Value);
                    }
                };
                contextMenu.Items.Add(navigateToTaskMenuItem);
            }

            // "Löschen" MenuItem
            var deleteMenuItem = new MenuItem
            {
                Header = "Benachrichtigung löschen",
                Icon = new TextBlock { Text = "🗑️", FontFamily = new System.Windows.Media.FontFamily("Segoe UI Emoji") }
            };
            deleteMenuItem.Click += (s, args) =>
            {
                if (DataContext is NewsViewModel viewModel)
                {
                    // Bestätigung anzeigen
                    var result = MessageBox.Show(
                        "Möchten Sie diese Benachrichtigung wirklich löschen?",
                        "Benachrichtigung löschen",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        viewModel.DeleteNotificationCommand?.Execute(notification);
                    }
                }
            };
            contextMenu.Items.Add(deleteMenuItem);

            // Kontextmenü anzeigen
            border.ContextMenu = contextMenu;
            contextMenu.IsOpen = true;
        }
    }

    /// <summary>
    /// Event-Handler für Laden der UserControl
    /// Initialisiert die News-Daten
    /// </summary>
    /// <param name="sender">Die UserControl</param>
    /// <param name="e">Routed-Event-Argumente</param>
    private void NewsView_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is NewsViewModel viewModel)
        {
            // Initial News laden
            viewModel.RefreshCommand?.Execute(null);
        }
    }

    /// <summary>
    /// Event-Handler für Tastatureingaben
    /// Ermöglicht Tastaturnavigation
    /// </summary>
    /// <param name="sender">Die UserControl</param>
    /// <param name="e">Key-Event-Argumente</param>
    private void NewsView_KeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is NewsViewModel viewModel)
        {
            switch (e.Key)
            {
                case Key.F5:
                    // F5 für Aktualisieren
                    viewModel.RefreshCommand?.Execute(null);
                    e.Handled = true;
                    break;
                case Key.A when Keyboard.Modifiers == ModifierKeys.Control:
                    // Ctrl+A für "Alle als gelesen markieren"
                    viewModel.MarkAllAsReadCommand?.Execute(null);
                    e.Handled = true;
                    break;
            }
        }
    }
}