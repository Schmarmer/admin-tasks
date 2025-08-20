using CommunityToolkit.Mvvm.ComponentModel;

namespace Admin_Tasks.ViewModels;

public abstract class BaseViewModel : ObservableObject
{
    private bool _isBusy;
    private string _title = string.Empty;

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    protected virtual void OnBusyChanged()
    {
        // Override in derived classes if needed
    }

    protected async Task ExecuteAsync(Func<Task> operation)
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            OnBusyChanged();
            await operation();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[BaseViewModel.ExecuteAsync] Exception: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[BaseViewModel.ExecuteAsync] StackTrace: {ex.StackTrace}");
            throw; // Re-throw to maintain original behavior
        }
        finally
        {
            IsBusy = false;
            OnBusyChanged();
        }
    }

    protected async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
    {
        if (IsBusy)
            return default(T)!;

        try
        {
            IsBusy = true;
            OnBusyChanged();
            return await operation();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[BaseViewModel.ExecuteAsync<T>] Exception: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[BaseViewModel.ExecuteAsync<T>] StackTrace: {ex.StackTrace}");
            throw; // Re-throw to maintain original behavior
        }
        finally
        {
            IsBusy = false;
            OnBusyChanged();
        }
    }
}