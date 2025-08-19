using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Admin_Tasks.Models;
using Admin_Tasks.Services;
using TaskStatus = Admin_Tasks.Models.TaskStatus;

namespace Admin_Tasks.ViewModels;

public class TaskCompletionViewModel : INotifyPropertyChanged
{
    private readonly ITaskService _taskService;
    private readonly IAttachmentService _attachmentService;
    private readonly IAuthenticationService _authService;
    private readonly TaskItem _task;
    
    private int _hours;
    private int _minutes;
    private string _completionText = string.Empty;
    private string _additionalNotes = string.Empty;
    private int _difficultyRating = 1;
    private int _satisfactionRating = 1;
    private string? _completionImagePath;
    private string? _completionImageName;
    private BitmapImage? _completionImagePreview;
    private bool _isCompleting;
    
    public TaskCompletionViewModel(TaskItem task, ITaskService taskService, IAttachmentService attachmentService, IAuthenticationService authService)
    {
        _task = task ?? throw new ArgumentNullException(nameof(task));
        _taskService = taskService ?? throw new ArgumentNullException(nameof(taskService));
        _attachmentService = attachmentService ?? throw new ArgumentNullException(nameof(attachmentService));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        
        // Initialize with default values
        _hours = 0;
        _minutes = 30; // Default to 30 minutes
    }
    
    public string TaskTitle => _task.Title;
    public string TaskDescription => _task.Description;
    
    public int Hours
    {
        get => _hours;
        set
        {
            if (value >= 0 && value <= 999) // Reasonable upper limit
            {
                SetProperty(ref _hours, value);
                OnPropertyChanged(nameof(TotalMinutes));
                OnPropertyChanged(nameof(TotalTimeText));
                OnPropertyChanged(nameof(CanComplete));
            }
        }
    }
    
    public int Minutes
    {
        get => _minutes;
        set
        {
            if (value >= 0 && value <= 59)
            {
                SetProperty(ref _minutes, value);
                OnPropertyChanged(nameof(TotalMinutes));
                OnPropertyChanged(nameof(TotalTimeText));
                OnPropertyChanged(nameof(CanComplete));
            }
        }
    }
    
    public int TotalMinutes => (_hours * 60) + _minutes;
    
    public string TotalTimeText
    {
        get
        {
            if (TotalMinutes == 0)
                return "Keine Zeit erfasst";
            
            var hours = TotalMinutes / 60;
            var minutes = TotalMinutes % 60;
            
            if (hours > 0 && minutes > 0)
                return $"Gesamt: {hours}h {minutes}min";
            else if (hours > 0)
                return $"Gesamt: {hours}h";
            else
                return $"Gesamt: {minutes}min";
        }
    }
    
    public string CompletionText
    {
        get => _completionText;
        set
        {
            SetProperty(ref _completionText, value ?? string.Empty);
            OnPropertyChanged(nameof(CanComplete));
        }
    }
    
    public string AdditionalNotes
    {
        get => _additionalNotes;
        set => SetProperty(ref _additionalNotes, value ?? string.Empty);
    }
    
    public int DifficultyRating
    {
        get => _difficultyRating;
        set
        {
            if (value >= 1 && value <= 5)
            {
                SetProperty(ref _difficultyRating, value);
                OnPropertyChanged(nameof(CanComplete));
            }
        }
    }
    
    public int SatisfactionRating
    {
        get => _satisfactionRating;
        set
        {
            if (value >= 1 && value <= 5)
            {
                SetProperty(ref _satisfactionRating, value);
                OnPropertyChanged(nameof(CanComplete));
            }
        }
    }
    
    public string? CompletionImagePath
    {
        get => _completionImagePath;
        private set => SetProperty(ref _completionImagePath, value);
    }
    
    public string? CompletionImageName
    {
        get => _completionImageName;
        private set => SetProperty(ref _completionImageName, value);
    }
    
    public BitmapImage? CompletionImagePreview
    {
        get => _completionImagePreview;
        private set => SetProperty(ref _completionImagePreview, value);
    }
    
    public bool HasCompletionImage => !string.IsNullOrEmpty(CompletionImagePath);
    
    public bool IsCompleting
    {
        get => _isCompleting;
        private set
        {
            SetProperty(ref _isCompleting, value);
            OnPropertyChanged(nameof(CanComplete));
        }
    }
    
    public bool CanComplete => 
        !IsCompleting &&
        TotalMinutes > 0 &&
        !string.IsNullOrWhiteSpace(CompletionText) &&
        DifficultyRating >= 1 && DifficultyRating <= 5 &&
        SatisfactionRating >= 1 && SatisfactionRating <= 5;
    
    public void SetCompletionImage(string filePath, string fileName)
    {
        try
        {
            CompletionImagePath = filePath;
            CompletionImageName = fileName;
            
            // Create preview image
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(filePath);
            bitmap.DecodePixelWidth = 200; // Resize for preview
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            
            CompletionImagePreview = bitmap;
            
            OnPropertyChanged(nameof(HasCompletionImage));
        }
        catch (Exception ex)
        {
            // Reset image properties on error
            CompletionImagePath = null;
            CompletionImageName = null;
            CompletionImagePreview = null;
            OnPropertyChanged(nameof(HasCompletionImage));
            
            throw new InvalidOperationException($"Fehler beim Laden des Bildes: {ex.Message}", ex);
        }
    }
    
    public void RemoveCompletionImage()
    {
        CompletionImagePath = null;
        CompletionImageName = null;
        CompletionImagePreview = null;
        OnPropertyChanged(nameof(HasCompletionImage));
    }
    
    public bool ValidateInput()
    {
        return TotalMinutes > 0 &&
               !string.IsNullOrWhiteSpace(CompletionText) &&
               DifficultyRating >= 1 && DifficultyRating <= 5 &&
               SatisfactionRating >= 1 && SatisfactionRating <= 5;
    }
    
    public bool HasUnsavedChanges()
    {
        return TotalMinutes > 0 ||
               !string.IsNullOrWhiteSpace(CompletionText) ||
               !string.IsNullOrWhiteSpace(AdditionalNotes) ||
               DifficultyRating != 1 ||
               SatisfactionRating != 1 ||
               HasCompletionImage;
    }
    
    public async Task<bool> CompleteTaskAsync()
    {
        if (!ValidateInput())
        {
            return false;
        }
        
        try
        {
            IsCompleting = true;
            
            // Update task status
            _task.Status = TaskStatus.Completed;
            _task.CompletedAt = DateTime.UtcNow;
            _task.UpdatedAt = DateTime.UtcNow;
            
            // Create completion details
            var currentUserId = _authService.CurrentUser?.Id ?? 0;
            if (currentUserId == 0)
            {
                System.Diagnostics.Debug.WriteLine("Fehler: Kein angemeldeter Benutzer gefunden");
                return false;
            }
            
            var completionDetails = new TaskCompletionDetails
            {
                TaskId = _task.Id,
                TimeSpentMinutes = TotalMinutes,
                ConclusionText = CompletionText.Trim(),
                AdditionalNotes = AdditionalNotes?.Trim() ?? string.Empty,
                DifficultyRating = DifficultyRating,
                SatisfactionRating = SatisfactionRating,
                CompletedAt = _task.CompletedAt.Value,
                CompletedByUserId = currentUserId
            };
            
            // Handle completion image if provided
            if (HasCompletionImage && !string.IsNullOrEmpty(CompletionImagePath))
            {
                try
                {
                    // Create attachment for completion image
                    var attachment = await _attachmentService.SaveAttachmentAsync(
                        File.ReadAllBytes(CompletionImagePath),
                        Path.GetFileName(CompletionImagePath),
                        "image/jpeg",
                        _task.Id,
                        _task.AssignedToUserId ?? 0
                    );
                    
                    if (attachment != null)
                    {
                        completionDetails.CompletionImagePath = attachment.FilePath;
                    }
                }
                catch (Exception ex)
                {
                    // Log error but don't fail the completion
                    System.Diagnostics.Debug.WriteLine($"Fehler beim Speichern des Abschlussbildes: {ex.Message}");
                }
            }
            
            // Update task with completion details
            var success = await _taskService.CompleteTaskWithDetailsAsync(_task, completionDetails);
            
            return success;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Fehler beim Abschließen der Aufgabe: {ex.Message}");
            return false;
        }
        finally
        {
            IsCompleting = false;
        }
    }
    
    public string GetDifficultyText()
    {
        return DifficultyRating switch
        {
            1 => "Sehr einfach",
            2 => "Einfach",
            3 => "Mittel",
            4 => "Schwer",
            5 => "Sehr schwer",
            _ => "Unbekannt"
        };
    }
    
    public string GetSatisfactionText()
    {
        return SatisfactionRating switch
        {
            1 => "Sehr unzufrieden",
            2 => "Unzufrieden",
            3 => "Neutral",
            4 => "Zufrieden",
            5 => "Sehr zufrieden",
            _ => "Unbekannt"
        };
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}