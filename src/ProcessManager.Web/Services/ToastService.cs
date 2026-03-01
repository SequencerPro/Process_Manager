namespace ProcessManager.Web.Services;

public enum ToastType { Success, Danger, Warning, Info }

public record ToastMessage(Guid Id, string Message, ToastType Type, int DurationMs);

/// <summary>
/// Circuit-scoped service for showing auto-dismissing toast notifications.
/// Inject and call ShowSuccess/ShowError from any page or component.
/// </summary>
public class ToastService
{
    private readonly List<ToastMessage> _toasts = new();

    public IReadOnlyList<ToastMessage> Toasts => _toasts;

    /// Fires whenever the toast list changes so ToastContainer can re-render.
    public event Action? OnChange;

    public void ShowSuccess(string message, int durationMs = 4000) => Show(message, ToastType.Success, durationMs);
    public void ShowError  (string message, int durationMs = 6000) => Show(message, ToastType.Danger,  durationMs);
    public void ShowWarning(string message, int durationMs = 5000) => Show(message, ToastType.Warning, durationMs);
    public void ShowInfo   (string message, int durationMs = 4000) => Show(message, ToastType.Info,    durationMs);

    public void Show(string message, ToastType type, int durationMs = 4000)
    {
        _toasts.Add(new ToastMessage(Guid.NewGuid(), message, type, durationMs));
        OnChange?.Invoke();
    }

    public void Dismiss(Guid id)
    {
        _toasts.RemoveAll(t => t.Id == id);
        OnChange?.Invoke();
    }
}
